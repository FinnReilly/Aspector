using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspector.Core.Attributes.Logging
{
    public class AddLogPropertyAttribute : AspectAttribute
    {
        public string LoggingContextKey { get; }
        public string? LoggableParameterName { get; }
        public object? ConstantValue { get; set; }
        public bool IsConstant { get; }

        public AddLogPropertyAttribute(string contextKey, string loggableParameterName)
        {
            LoggingContextKey = contextKey;
            LoggableParameterName = loggableParameterName;
        }

        public AddLogPropertyAttribute(string contextKey)
        {
            LoggingContextKey = contextKey;
            IsConstant = true;
        }
    }
}
