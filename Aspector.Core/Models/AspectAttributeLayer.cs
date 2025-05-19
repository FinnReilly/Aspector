using Aspector.Core.Attributes;

namespace Aspector.Core.Models
{
    public class AspectAttributeLayer : List<AspectAttribute>
    {
        public int LayerIndex { get; set; }
        public Type AspectType { get; }

        public AspectAttributeLayer(int layerIndex, AspectAttribute firstMember)
        {
            LayerIndex = layerIndex;
            AspectType = firstMember.GetType();
            Add(firstMember);
        }

        public static AspectAttributeLayer FromReversed(AspectAttributeLayer layerToReverse)
        {
            var reversedRange = layerToReverse.Reverse<AspectAttribute>();
            var reversedLayer = new AspectAttributeLayer(layerToReverse.LayerIndex, reversedRange.First());
            reversedLayer.AddRange(reversedRange.Skip(1));

            return reversedLayer;
        }
    }
}
