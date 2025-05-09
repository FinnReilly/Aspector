using Aspector.Core.Attributes;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Aspector.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAspects(this IServiceCollection services)
        {
            services.AddSingleton(new ProxyGenerator());
            if (!services.Any(s => s.ServiceType == typeof(IMemoryCache)))
            {
                services.AddMemoryCache();
            }

            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());

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
            var baseAspectType = typeof(BaseAspectImplementation<>);
            var assignableTypes = usedAttributes.Select(t => baseAspectType.MakeGenericType([t]));
            var requiredImplementationTypes = allTypes.Where(
                t =>
                    { 
                        if (t.IsAbstract)
                        {
                            return false;
                        }

                        var matchingAssignableType = assignableTypes.Where(aType => t.IsAssignableTo(aType)).FirstOrDefault();
                        if (matchingAssignableType == null)
                        {
                            return false;
                        }

                        var aspectArgumentType = matchingAssignableType
                            .GetGenericArguments()
                            .First(arg => arg.IsAssignableTo(typeof(AspectAttribute)));

                        if (usedAttributes.Contains(aspectArgumentType))
                        {
                            if (implementationDictionary.TryGetValue(aspectArgumentType, out var existingImplementation))
                            {
                                throw new InvalidOperationException(
                                    $"Only one aspect implementation can inherit from BaseAspectImplementation<{aspectArgumentType.FullName}>," +
                                    $" but both {existingImplementation.FullName} and {t.FullName} implement this abstract class");
                            }

                            implementationDictionary.Add(aspectArgumentType, t);
                            return true;
                        }

                        return false;
                    }).ToList();


            foreach(var attributeType in usedAttributes)
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
                descriptor => descriptor.ServiceType.IsInterface
                    && decoratedTypes.Contains(descriptor.ImplementationType!));

            foreach(var serviceDescriptor in applicableServicesInContainer)
            {
                var applicableAspects = serviceDescriptor
                    .ImplementationType!
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .SelectMany(m => m.CustomAttributes.Where(a => a.AttributeType.IsAssignableTo(typeof(AspectAttribute))))
                    .Select(a => a.AttributeType)
                    .Distinct();


                services.Replace(new ServiceDescriptor(
                    serviceDescriptor.ServiceType,
                    factory: (provider) =>
                    {
                        var generator = provider.GetRequiredService<ProxyGenerator>();
                        var target = provider.GetRequiredService(serviceDescriptor.ServiceType);
                        var aspectImplementations = applicableAspects.Select(aspect => (IInterceptor)provider.GetRequiredService(implementationDictionary[aspect])).ToArray();

                        return generator.CreateInterfaceProxyWithTarget(serviceDescriptor.ServiceType, target: target, interceptors: aspectImplementations);
                    },
                    serviceDescriptor.Lifetime));
            }
            
            return services;
        }
    }
}
