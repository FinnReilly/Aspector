using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspector.Core.Exceptions
{
    public class LifetimeMismatchException : Exception
    {
        public Type DecoratedType { get; }
        public Type DecoratorType { get; }
        public ServiceLifetime DecoratorLifetime { get; }

        public LifetimeMismatchException(Type decoratedType, Type decoratorType, ServiceLifetime decoratorLifetime)
            : base($"{decoratorType.FullName} cannot be used to decorate methods in {decoratedType.FullName}, as decorators with a {decoratorLifetime} lifetime cannot decorate a singleton")
        {
            DecoratedType = decoratedType;
            DecoratorType = decoratorType;
            DecoratorLifetime = decoratorLifetime;
        }
    }
}
