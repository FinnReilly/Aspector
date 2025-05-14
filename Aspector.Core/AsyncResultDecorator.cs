using Aspector.Core.Attributes;

namespace Aspector.Core
{
    public abstract class AsyncResultDecorator<TAspect, TFinalResult> : ResultDecorator<TAspect, Task<TFinalResult>>
        where TAspect : AspectAttribute
    {
    }
}
