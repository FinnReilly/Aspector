using Aspector.Core.Attributes;
using Aspector.Core.Services;

namespace Aspector.Core
{
    public abstract class AsyncDecorator<TAspect> : ResultDecorator<TAspect, Task>
        where TAspect : AspectAttribute
    {
        protected AsyncDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }
    }
}
