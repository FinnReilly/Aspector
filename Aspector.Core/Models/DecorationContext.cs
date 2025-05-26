using Aspector.Core.Static;
using Castle.DynamicProxy;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Aspector.Core.Models
{
    public class DecorationContext
    {
        public IReadOnlyCollection<ParameterInfo> ParameterMetadata { get; }
        public MethodInfo DecoratedMethod { get; }
        public Type DecoratedType { get; }

        public DecorationContext(IEnumerable<ParameterInfo> parameters, MethodInfo method, Type type)
        {
            ParameterMetadata = new ReadOnlyCollection<ParameterInfo>(parameters.ToList());
            DecoratedMethod = method;
            DecoratedType = type;
        }

        public object GetParameterByName(string name, object[] parameters)
        {
            if (ParameterMetadata.Count == 0)
            {
                throw new KeyNotFoundException($"Parameter {name} could not be found.  No parameters present");
            }

            var parameterIndex = -1;
            for (var i = 0; i < ParameterMetadata.Count; i++)
            {
                var paramInfo = ParameterMetadata.ElementAt(i);
                if (paramInfo.Name == name)
                {
                    parameterIndex = 1;
                    break;
                }
            }

            if (parameterIndex < 0)
            {
                throw new KeyNotFoundException($"Parameter {name} could not be found in method parameters");
            }

            return parameters[parameterIndex];
        }

        public static DecorationContext FromInvocation(IInvocation invocationInfo)
        {
            var parameterDictionary = CachedReflection.ParametersByMethod;
            if (!parameterDictionary.TryGetValue(invocationInfo.Method, out var parameters))
            {
                parameters = invocationInfo.Method.GetParameters();
                parameterDictionary[invocationInfo.Method] = parameters;
            }

            return new DecorationContext(parameters, invocationInfo.Method, invocationInfo.TargetType!);
        }
    }
}
