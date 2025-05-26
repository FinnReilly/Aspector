using Aspector.Core.Attributes.Logging;
using Aspector.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aspector.Core.Logging
{
    public class LogAspect : VoidDecorator<LogAttribute>
    {
        public LogAspect(ILoggerFactory loggerFactory, int layerIndex) : base(loggerFactory, layerIndex)
        {
        }

        protected override void Decorate(
            Action<object[]?> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<LogAttribute> aspectParameters)
        {
            var logger = GetLogger(context);
            parameters ??= Array.Empty<object>();
            foreach (var param in aspectParameters)
            {
                logger.Log(
                    param.Level,
                    param.LogString,
                    param.ParametersForLogging.Select(name => context.GetParameterByName(name, parameters)));
            }

            targetMethod(parameters);
        }
    }
}
