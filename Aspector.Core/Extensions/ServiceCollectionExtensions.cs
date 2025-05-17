using Aspector.Core.Attributes;
using Aspector.Core.Static;
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

            var usedAttributeTypes = new HashSet<Type>();
            var decoratedTypes = services
                .Where(
                    service =>
                        {
                            var t = service.ImplementationType;
                            if (t == null)
                            {
                                return false;
                            }

                            var decoratedMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                            if (!decoratedMethods.Any())
                            {
                                return false;
                            }

                            foreach(var method in decoratedMethods)
                            {
                                var decorators = method.GetCustomAttributes(inherit: true)
                                    .Select(a => (Attr: a, AttrType: a.GetType()))
                                    .Where(attr => attr.AttrType.IsAssignableTo(typeof(AspectAttribute)));

                                if (decorators.Any())
                                {
                                    foreach (var decorator in decorators)
                                    {
                                        usedAttributeTypes.Add(decorator.AttrType);
                                    }
                                }

                                CachedReflection.AttributeSummariesByMethod[method] = new Models.AspectAttributeSummary(
                                    decorators.Select(a => a.Attr)
                                        .Cast<AspectAttribute>()
                                        .ToArray());
                            }
                            
                            return true;
                        })
                .Select(s => s.ImplementationType)
                .ToHashSet();

            var implementationDictionary = new Dictionary<Type, Type>();
            var baseAspectType = typeof(BaseDecorator<>);
            var assignableTypes = usedAttributeTypes.Select(t => baseAspectType.MakeGenericType([t]));
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

                        if (usedAttributeTypes.Contains(aspectArgumentType))
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

            foreach(var attributeType in usedAttributeTypes)
            {
                if (!implementationDictionary.ContainsKey(attributeType))
                {
                    throw new NotImplementedException($"There is no aspect implementation corresponding to the aspect attribute {attributeType.FullName}");
                }
            }

            var summarisedMaximumLayerMaps = AggregateMaximumIndex(CachedReflection.AttributeSummariesByMethod.Values.Select(summary => summary.MaximumIndexByType));

            // add required implementations, as singleton for now - add as keyed by required layer
            foreach (var kvp in summarisedMaximumLayerMaps)
            {
                var typeToRegister = implementationDictionary[kvp.Key];

                for (var maxLayerIndexForType = 0; maxLayerIndexForType <= kvp.Value; maxLayerIndexForType++)
                {
                    services.AddKeyedSingleton(typeToRegister, serviceKey: $"{typeToRegister.FullName}_{kvp.Value}");
                }
            }

            var applicableServicesInContainer = services.Where(
                descriptor => descriptor.ServiceType.IsInterface
                    && decoratedTypes.Contains(descriptor.ImplementationType!))
                .ToList();

            foreach(var serviceDescriptor in applicableServicesInContainer)
            {
                var applicableAspectMaxLayers = serviceDescriptor
                    .ImplementationType!
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(method => CachedReflection.AttributeSummariesByMethod[method].MaximumIndexByType);

                var maximumIndexByClass = AggregateMaximumIndex(applicableAspectMaxLayers);

                var replacement = ServiceDescriptor.Describe(
                    serviceDescriptor.ServiceType,
                    implementationFactory: (provider) =>
                    {
                        var generator = provider.GetRequiredService<ProxyGenerator>();
                        var target = ActivatorUtilities.CreateInstance(provider, serviceDescriptor.ImplementationType);
                        var aspectImplementations = applicableAspectMaxLayers.Select(aspect => (IInterceptor)provider.GetRequiredService(implementationDictionary[aspect])).ToArray();

                        return generator.CreateInterfaceProxyWithTarget(serviceDescriptor.ServiceType, target: target, interceptors: aspectImplementations);
                    },
                    lifetime: serviceDescriptor.Lifetime);

                var index = services.IndexOf(serviceDescriptor);
                services.Insert(index, replacement);
                services.Remove(serviceDescriptor);
            }
            
            return services;
        }

        private static Dictionary<Type, int> AggregateMaximumIndex(IEnumerable<Dictionary<Type, int>> maximumIndexDictionaries)
        {
            return maximumIndexDictionaries.Aggregate(
                    seed: new Dictionary<Type, int>(),
                    func: (totalSummary, dictionary) =>
                    {
                        foreach (var kvp in dictionary)
                        {
                            if (!totalSummary.TryGetValue(kvp.Key, out var domainMaxIndex))
                            {
                                totalSummary[kvp.Key] = kvp.Value;
                            }
                            else
                            {
                                totalSummary[kvp.Key] = Math.Max(kvp.Value, domainMaxIndex);
                            }
                        }

                        return totalSummary;
                    });
        }
    }
}
