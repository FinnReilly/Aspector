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
                        if (MaximumIndexByType.TryGetValue(targetAttributeType, out var previousLayerIndex))
                        {
                            thisLayerIndex = previousLayerIndex + 1;
                        }

                        MaximumIndexByType[targetAttributeType] = thisLayerIndex;
                        theseLayersFromInnermost.Add(new AspectAttributeLayer(thisLayerIndex, targetAttribute));
                    }
                    else
                    {
                        theseLayersFromInnermost.Last().Add(targetAttribute);
                    }
                }

                maxDepthByMethod[decoratedMethod] = theseLayersFromInnermost.Count;
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

        public Dictionary<Type, int> MaximumIndexByType = new Dictionary<Type, int>();
        public List<(Type AspectType, int LayerIndex)> WrapOrder = new List<(Type AspectType, int LayerIndex)>();
        public Dictionary<MethodInfo, List<AspectAttributeLayer>> LayersFromInnermostByMethod { get; } = new Dictionary<MethodInfo, List<AspectAttributeLayer>>();
        public Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>> LayersByType { get; } = new Dictionary<MethodInfo, Dictionary<Type, Dictionary<int, AspectAttributeLayer>>>();
    }
}
