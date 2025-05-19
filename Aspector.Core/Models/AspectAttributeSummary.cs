using Aspector.Core.Attributes;
using System.Reflection;

namespace Aspector.Core.Models
{
    public class AspectAttributeSummary
    {
        public AspectAttributeSummary((MethodInfo method, AspectAttribute[] aspects)[] inputs)
        {
            Type? lastType = null;
            var maxDepthByMethod = new Dictionary<MethodInfo, int>();
            foreach (var input in inputs)
            {
                var (decoratedMethod, aspectAttributes) = input;
                var theseLayersFromInnermost = new List<AspectAttributeLayer>();
                LayersFromInnermostByMethod[decoratedMethod] = theseLayersFromInnermost;

                for (var d = aspectAttributes.Length - 1; d >= 0; d--)
                {
                    var targetAttribute = aspectAttributes[d];
                    var targetAttributeType = targetAttribute.GetType();
                    if (lastType != targetAttributeType)
                    {
                        // mark new layer
                        lastType = targetAttributeType;
                        var thisLayerIndex = 0;
                        theseLayersFromInnermost.Add(new AspectAttributeLayer(thisLayerIndex, targetAttribute));
                    }
                    else
                    {
                        theseLayersFromInnermost.Last().Add(targetAttribute);
                    }
                }

                maxDepthByMethod[decoratedMethod] = theseLayersFromInnermost.Count;
            }

            var maxDepth = maxDepthByMethod.Max(kvp => kvp.Value);
            var analysisStructure = inputs.Select(i => LayersFromInnermostByMethod[i.method]).ToList();
            var typesNotAssignedAWrapLayer = new Dictionary<Type, int>();
            
            for (var i = 0; i <= maxDepth; i++)
            {
                var rowToAnalyse = analysisStructure.Where(list => list.Count > i).Select(list => list[i]);
                var groupingsByType = rowToAnalyse.GroupBy(layer => layer.AspectType);
                var typeCountsForThisRow = groupingsByType.GroupBy(g => g.Count()).OrderBy(g => g.Key);

                foreach (var unassignedType in typesNotAssignedAWrapLayer)
                {
                    if (!groupingsByType.Any(g => g.Key == unassignedType.Key))
                    {
                        AddToWrapOrder(unassignedType.Key);
                        typesNotAssignedAWrapLayer.Remove(unassignedType.Key);
                    }
                }

                foreach (var typeCountForThisAnalysisRow in typeCountsForThisRow)
                {
                    foreach (var typeWithThisCount in typeCountForThisAnalysisRow)
                    {
                        var countForType = typeCountForThisAnalysisRow.Key;
                        var aspectType = typeWithThisCount.Key;

                        if (!typesNotAssignedAWrapLayer.TryGetValue(aspectType, out var countUnassigned))
                        {
                            countUnassigned = 0;
                        }
                        typesNotAssignedAWrapLayer[aspectType] = countUnassigned + countForType;
                    }
                }
            }

            //LayersByType = LayersFromInnermostByMethod.Aggregate(
            //    seed: new Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>>(),
            //    func: (layersByType, singleLayer) => 
            //    {
            //        if (!layersByType.TryGetValue())
            //        if (!layersByType.TryGetValue(singleLayer.AspectType, out var layersForType))
            //        {
            //            layersForType = new Dictionary<int, AspectAttributeLayer>();
            //            layersByType[singleLayer.AspectType] = layersForType;
            //        }

            //        layersForType[singleLayer.LayerIndex] = AspectAttributeLayer.FromReversed(singleLayer);

            //        return layersByType;
            //    });
        }

        private void AddToWrapOrder(Type type)
        {
            if (!MaximumIndexByType.TryGetValue(type, out var currentMaxIndex))
            {
                currentMaxIndex = -1;
            }
            MaximumIndexByType[type] = currentMaxIndex + 1;

            WrapOrder.Add((AspectType: type, LayerIndex: MaximumIndexByType[type]));
        }

        public Dictionary<Type, int> MaximumIndexByType = new Dictionary<Type, int>();
        public List<(Type AspectType, int LayerIndex)> WrapOrder = new List<(Type AspectType, int LayerIndex)>();
        public Dictionary<MethodInfo, List<AspectAttributeLayer>> LayersFromInnermostByMethod { get; } = new Dictionary<MethodInfo, List<AspectAttributeLayer>>();
        public Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>> LayersByType { get; } = new Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>>();
    }
}
