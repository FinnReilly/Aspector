using Aspector.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace Aspector.Core
{
    public abstract class AsyncDecorator<TAspect> : ResultDecorator<TAspect, Task>
        where TAspect : AspectAttribute
    {
        protected AsyncDecorator(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
        }
    }
}
