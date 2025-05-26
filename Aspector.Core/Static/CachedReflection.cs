using Aspector.Core.Models.Registration;
using System.Collections.Concurrent;
using System.Reflection;

namespace Aspector.Core.Static
{
    public static class CachedReflection
    {
        public static ConcurrentDictionary<MethodInfo, ParameterInfo[]> ParametersByMethod = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();
        public static Dictionary<Type, AspectAttributeSummary> AttributeSummariesByClass = new Dictionary<Type, AspectAttributeSummary>();
    }
}
