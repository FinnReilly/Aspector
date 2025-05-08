using Aspector.Core.Attributes;
using Aspector.Core.Implementations;
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

            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var usedAttributes = new HashSet<Type>();
            var decoratedTypes = allTypes
                .Where(
                    t =>
                        {
                            var decorators = t.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                                .SelectMany(m => m.CustomAttributes.Where(attr => attr.AttributeType.IsAssignableTo(typeof(AspectAttribute))));
                            
                            if (decorators.Any())
                            {
                                foreach(var decorator in decorators)
                                {
                                    usedAttributes.Add(decorator.AttributeType);
                                }

                                return true;
                            }

                            return false;
                        })
                .ToHashSet();

            var implementationDictionary = new Dictionary<Type, Type>();
            var requiredImplementationTypes = allTypes.Where(
                t =>
                    { 
                        if (t.IsAbstract || !t.IsAssignableTo(typeof(BaseAspectImplementation<>)))
                        {
                            return false;
                        }

                        var aspectArgumentType = t.GetGenericArguments().First(arg => arg.IsAssignableTo(typeof(AspectAttribute)));
                        if (usedAttributes.Contains(aspectArgumentType))
                        {
                            implementationDictionary.Add(aspectArgumentType, t);
                            return true;
                        }

                        return false;
                    });


            foreach(var attributeType in decoratedTypes)
            {
                if (!implementationDictionary.ContainsKey(attributeType))
                {
                    throw new NotImplementedException($"There is no aspect implementation corresponding to the aspect attribute {attributeType.FullName}");
                }
            }

            // add required implementations, as singleton for now
            foreach (var type in requiredImplementationTypes)
            {
                services.AddSingleton(type);
            }

            var applicableServicesInContainer = services.Where(
                descriptor => descriptor.ImplementationInstance != null 
                    && descriptor.ServiceType.IsInterface
                    && decoratedTypes.Contains(descriptor.ImplementationType!));

            foreach(var serviceDescriptor in applicableServicesInContainer)
            {
                var applicableAspects = serviceDescriptor
                    .ImplementationType!
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .SelectMany(m => m.CustomAttributes.Where(a => a.AttributeType.IsAssignableTo(typeof(AspectAttribute))))
                    .Select(a => a.AttributeType)
                    .Distinct();

                services.Remove(serviceDescriptor);

                services.Add(new ServiceDescriptor(
                    serviceDescriptor.ServiceType,
                    factory: (provider) =>
                    {
                        var generator = provider.GetRequiredService<ProxyGenerator>();
                        var aspectImplementations = applicableAspects.Select(aspect => provider.GetRequiredService(implementationDictionary[aspect]));

                        return generator.CreateInterfaceProxyWithTarget(serviceDescriptor.ServiceType, aspectImplementations);
                    },
                    serviceDescriptor.Lifetime));
            }
            
            return services;
        }
    }
}
