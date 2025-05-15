using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aspector.Core.Static
{
    public static class CachedReflection
    {
        public static ConcurrentDictionary<MethodInfo, ParameterInfo[]> ParametersByMethod = new ConcurrentDictionary<MethodInfo, ParameterInfo[]>();
    }
}
