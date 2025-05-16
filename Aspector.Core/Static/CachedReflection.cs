using Aspector.Core.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace Aspector.Core.Static
{
    public static class CachedReflection
    {
        public static ConcurrentDictionary<MethodInfo, ParameterInfo[]> ParametersByMethod = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();
        public static Dictionary<MethodInfo, AspectAttributeSummary> AttributeSummariesByMethod = new Dictionary<MethodInfo, AspectAttributeSummary>();
    }
}
