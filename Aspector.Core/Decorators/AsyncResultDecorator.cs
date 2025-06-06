using Aspector.Core.Attributes;
using Aspector.Core.Services;

namespace Aspector.Core.Decorators
{
    public abstract class AsyncResultDecorator<TAspect, TFinalResult> : ResultDecorator<TAspect, Task<TFinalResult>>
        where TAspect : AspectAttribute
    {
        protected AsyncResultDecorator(IDecoratorServices services, int layerIndex) 
            : base(services, layerIndex)
        {
        }
    }
}
