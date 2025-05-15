using Aspector.Core.Attributes;

namespace Aspector.Core
{
    public abstract class AsyncDecorator<TAspect> : ResultDecorator<TAspect, Task>
        where TAspect : AspectAttribute
    {
    }
}
