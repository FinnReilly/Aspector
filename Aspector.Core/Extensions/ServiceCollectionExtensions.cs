using Aspector.Core.Attributes;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Aspector.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAspects(this IServiceCollection services)
        {
            services.AddSingleton(new ProxyGenerator());

            var decoratedTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(
                    t =>
                        t.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                            .Any(m => m.GetCustomAttributes().Any(attr => attr.GetType().IsAssignableTo(typeof(AspectAttribute)))))
                .ToHashSet();

            var applicableServicesInContainer = services.Where(
                descriptor => descriptor.ImplementationInstance != null 
                    && descriptor.ServiceType.IsInterface
                    && decoratedTypes.Contains(descriptor.ImplementationType!));

            foreach(var serviceDescriptor in applicableServicesInContainer)
            {
                var applicableAspects = serviceDescriptor
                    .ImplementationType!
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .SelectMany(m => m.CustomAttributes.Where(a => a.AttributeType.IsAssignableTo(typeof(AspectAttribute))));

                services.Remove(serviceDescriptor);

                services.Add(new ServiceDescriptor(
                    serviceDescriptor.ServiceType,
                    factory: (provider) =>
                    {
                        var generator = provider.GetRequiredService<ProxyGenerator>();

                        return generator.CreateInterfaceProxyWithTarget(serviceDescriptor.ServiceType, applicableAspects);
                    },
                    serviceDescriptor.Lifetime));
            }
            
            return services;
        }
    }
}
