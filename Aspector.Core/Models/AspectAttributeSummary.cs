using Aspector.Core.Attributes;

namespace Aspector.Core.Models
{
    public class AspectAttributeSummary
    {
        public AspectAttributeSummary(AspectAttribute[] inputs)
        {
            Type? lastType = null;
            for (var d = inputs.Length - 1; d >= 0; d--)
            {
                var targetAttribute = inputs[d];
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
                    LayersFromInnermost.Add(new AspectAttributeLayer(thisLayerIndex, targetAttribute));
                }
                else
                {
                    LayersFromInnermost.Last().Add(targetAttribute);
                }
            }

            LayersByType = LayersFromInnermost.Aggregate(
                seed: new Dictionary<Type, Dictionary<int, AspectAttributeLayer>>(),
                func: (layersByType, singleLayer) => 
                {
                    if (!layersByType.TryGetValue(singleLayer.AspectType, out var layersForType))
                    {
                        layersForType = new Dictionary<int, AspectAttributeLayer>();
                        layersByType[singleLayer.AspectType] = layersForType;
                    }

                    layersForType[singleLayer.LayerIndex] = AspectAttributeLayer.FromReversed(singleLayer);

                    return layersByType;
                });
        }

        Dictionary<Type, int> MaximumIndexByType = new Dictionary<Type, int>();
        List<AspectAttributeLayer> LayersFromInnermost { get; } = new List<AspectAttributeLayer>();
        Dictionary<Type, Dictionary<int, AspectAttributeLayer>> LayersByType { get; } = new Dictionary<Type, Dictionary<int, AspectAttributeLayer>>();
    }
}
