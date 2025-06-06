using Aspector.Core.Attributes.Logging;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Microsoft.Extensions.Logging;

namespace Aspector.Core.Decorators.Logging
{
    public class LogDecorator : VoidDecorator<LogAttribute>
    {
        public LogDecorator(IDecoratorServices services, int layerIndex) : base(services, layerIndex)
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
