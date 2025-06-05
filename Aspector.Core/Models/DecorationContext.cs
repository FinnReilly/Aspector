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

        public bool TrySetParameterByName(string name, object[] parameters, object? parameter)
            => TrySetParameterByName<object>(name, parameters, parameter);

        public bool TrySetParameterByName<TParam>(string name, object[] parameters, TParam? param)
        {
            var parameterIndex = TryGetParameterByName<TParam>(name, parameters, out var _);
            if (parameterIndex < 0)
            {
                return false;
            }

            parameters[parameterIndex] = param!;
            return true;
        }

        public object? GetParameterByName(string name, object[] parameters) => GetParameterByName<object>(name, parameters);

        public int TryGetParameterByName(string name, object[] parameters, out object? parameter)
            => TryGetParameterByName<object>(name, parameters, out parameter);

        public int TryGetParameterByName<TParam>(string name, object?[] parameters, out TParam? param)
        {
            param = default;
            var parameterIndex = -1;

            if (ParameterMetadata.Count == 0)
            {
                throw new KeyNotFoundException($"Parameter {name} could not be found.  No parameters required for {DecoratedMethod.Name}");
            }

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

            param = (TParam?)parameters[parameterIndex];
            return parameterIndex;
        }

        public TParam? GetParameterByName<TParam>(string name, object[] parameters)
        {
            TryGetParameterByName<TParam>(name, parameters, out var foundParam);
            return foundParam;
        }

        public TParam? GetFirstOrDefault<TParam>(object?[] parameters, bool returnDefaultForMultiple = false)
        {
            TryGetFirstOrDefault<TParam>(parameters, out var foundParam, returnDefaultForMultiple);
            return foundParam;
        }

        public bool TrySetFirst<TParam>(object?[] parameters, TParam value, bool failIfMultiple = false)
        {
            var parameterIndex = TryGetFirstOrDefault<TParam>(parameters, out var _, failIfMultiple);
            
            if (parameterIndex < 0)
            {
                return false;
            }

            parameters[parameterIndex] = value;
            return true;
        }

        public int TryGetFirstOrDefault<TParam>(object?[] parameters, out TParam? foundParam, bool returnDefaultForMultiple = false)
        {
            var foundParameterIndices = new List<int>();
            for (var i = 0; i < ParameterMetadata.Count && i < parameters.Length; i++)
            {
                var paramInfo = ParameterMetadata.ElementAt(i);
                if (paramInfo.ParameterType.IsAssignableTo(typeof(TParam)))
                {
                    foundParameterIndices.Add(i);
                }
            }

            if (foundParameterIndices.Count == 0
                || (returnDefaultForMultiple && foundParameterIndices.Count > 1))
            {
                foundParam = default;
                return -1;
            }

            foundParam = (TParam)parameters[foundParameterIndices[0]]!;
            return foundParameterIndices[0];
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
