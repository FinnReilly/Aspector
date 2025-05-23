using Aspector.Core.Attributes.Logging;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
            (ParameterInfo[] ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) context,
            IEnumerable<LogAttribute> aspectParameters)
        {
            var logger = GetLogger(context.DecoratedType);
            parameters ??= Array.Empty<object>();
            foreach (var param in aspectParameters)
            {
                logger.Log(
                    param.Level,
                    param.LogString,
                    param.ParametersForLogging.Select(name => GetParameterByName(name, context.ParameterMetadata, parameters)));
            }

            targetMethod(parameters);
        }
    }
}
