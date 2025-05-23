using Microsoft.Extensions.DependencyInjection;

namespace Aspector.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AspectLifetimeAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; }

        public AspectLifetimeAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }
    }
}
