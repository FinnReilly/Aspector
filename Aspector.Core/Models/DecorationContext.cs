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
        public CancellationToken CancellationToken { get; }
        public bool CancellationTokenIsGlobal { get; } = true;

        public DecorationContext(IEnumerable<ParameterInfo> parameters, MethodInfo method, Type type, CancellationToken globalCancellationToken, object?[]? receivedParameters = null)
        {
            receivedParameters ??= [];

            ParameterMetadata = new ReadOnlyCollection<ParameterInfo>(parameters.ToList());
            DecoratedMethod = method;
            DecoratedType = type;
            CancellationToken = globalCancellationToken;

            var singlePassedCancellationToken = GetFirstOrDefault<CancellationToken>(receivedParameters);

            if (singlePassedCancellationToken != default)
            {
                CancellationToken = singlePassedCancellationToken;
                CancellationTokenIsGlobal = false;
            }
        }

        public object GetParameterByName(string name, object[] parameters) => GetParameterByName<object>(name, parameters);

        public TParam GetParameterByName<TParam>(string name, object[] parameters)
        {
            if (ParameterMetadata.Count == 0)
            {
                throw new KeyNotFoundException($"Parameter {name} could not be found.  No parameters required for {DecoratedMethod.Name}");
            }

            var parameterIndex = -1;
            for (var i = 0; i < ParameterMetadata.Count; i++)
            {
                var paramInfo = ParameterMetadata.ElementAt(i);
                if (paramInfo.Name == name && paramInfo.ParameterType.IsAssignableTo(typeof(TParam)))
                {
                    parameterIndex = i;
                    break;
                }
            }

            if (parameterIndex < 0)
            {
                var typeDescription = typeof(TParam) == typeof(object) ? string.Empty : $", with type of {typeof(TParam).Name}";
                throw new KeyNotFoundException($"Parameter {name}{typeDescription} could not be found in method parameters for {DecoratedMethod.Name}");
            }

            return (TParam)parameters[parameterIndex];
        }

        public TParam? GetFirstOrDefault<TParam>(object?[] parameters, bool returnDefaultForMultiple = false)
        {
            var foundParameters = new List<int>();
            for (var i = 0; i < ParameterMetadata.Count && i < parameters.Length; i++)
            {
                var paramInfo = ParameterMetadata.ElementAt(i);
                if (paramInfo.ParameterType.IsAssignableTo(typeof(TParam)))
                {
                    foundParameters.Add(i);
                }
            }

            if (foundParameters.Count == 0
                || (returnDefaultForMultiple && foundParameters.Count > 1))
            {
                return default;
            }

            return (TParam)parameters[foundParameters[0]]!;
        }

        public static DecorationContext FromInvocation(IInvocation invocationInfo, CancellationToken globalToken)
        {
            var parameterDictionary = CachedReflection.ParametersByMethod;
            if (!parameterDictionary.TryGetValue(invocationInfo.Method, out var parameters))
            {
                parameters = invocationInfo.Method.GetParameters();
                parameterDictionary[invocationInfo.Method] = parameters;
            }

            return new DecorationContext(parameters, invocationInfo.Method, invocationInfo.TargetType!, globalToken, invocationInfo.Arguments!);
        }
    }
}
