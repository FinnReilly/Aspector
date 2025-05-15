using Aspector.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace Aspector.Core
{
    public abstract class AsyncResultDecorator<TAspect, TFinalResult> : ResultDecorator<TAspect, Task<TFinalResult>>
        where TAspect : AspectAttribute
    {
        protected AsyncResultDecorator(ILoggerFactory loggerFactory) 
            : base(loggerFactory)
        {
        }
    }
}
