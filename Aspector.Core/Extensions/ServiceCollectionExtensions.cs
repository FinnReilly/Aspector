using Aspector.Core.Attributes;
using Aspector.Core.Models;
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

            var usedAttributes = new HashSet<Type>();
            var applicableServicesInContainer = services
                .Where(
                    t =>
                        {
                            if (t.ImplementationType is null || !t.ServiceType.IsInterface)
                            {
                                return false;
                            }

                            var decoratedMethods = t.ImplementationType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                .Where(m => m.CustomAttributes.Any(attr => attr.AttributeType.IsAssignableTo(typeof(AspectAttribute<>))));
                            var decoratorTypes = decoratedMethods.SelectMany(m => m.CustomAttributes.Where(attr => attr.AttributeType.IsAssignableTo(typeof(AspectAttribute))));
                            
                            if (decoratorTypes.Any())
                            {
                                foreach(var decorator in decoratorTypes)
                                {
                                    usedAttributes.Add(decorator.AttributeType);
                                }

                                CachedReflection.AttributeSummariesByClass[t.ImplementationType] = new AspectAttributeSummary(
                                    decoratedMethods.Select(
                                        method => (
                                            method,
                                            method.GetCustomAttributes()
                                                .Where(a => usedAttributes.Contains(a.GetType()))
                                                .Cast<AspectAttribute>()
                                                .ToArray()))
                                    .ToArray());

                                return true;
                            }

                            return false;
                        })
                .ToHashSet();

            var implementationDictionary = new Dictionary<Type, Type>();
            var baseAspectType = typeof(BaseDecorator<>);
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

            // calculate domain-level max depth
            var maxDepth = CachedReflection.AttributeSummariesByClass.Aggregate(
                new Dictionary<Type, int>(),
                (maxCounters, summary) =>
                {
                    foreach (var counter in summary.Value.MaximumIndexByType)
                    {
                        if (!maxCounters.TryGetValue(counter.Key, out int counterValue))
                        {
                            counterValue = 0;
                        }

                        maxCounters[counter.Key] = Math.Max(counterValue, counter.Value);
                    }

                    return maxCounters;
                });

            // add required implementations, as singleton for now
            foreach (var type in requiredImplementationTypes)
            {
                var layersToAdd = maxDepth[type];

                for (var  i = 0; i < layersToAdd; i++)
                {
                    services.AddKeyedSingleton(type, i);
                }
            }

            foreach(var serviceDescriptor in applicableServicesInContainer)
            {
                var applicableAspects = serviceDescriptor
                    .ImplementationType!
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .SelectMany(m => m.CustomAttributes.Where(a => a.AttributeType.IsAssignableTo(typeof(AspectAttribute))))
                    .Select(a => a.AttributeType)
                    .Distinct();

                var replacement = ServiceDescriptor.Describe(
                    serviceDescriptor.ServiceType,
                    implementationFactory: (provider) =>
                    {
                        var generator = provider.GetRequiredService<ProxyGenerator>();
                        var target = ActivatorUtilities.CreateInstance(provider, serviceDescriptor.ImplementationType);
                        var aspectImplementations = applicableAspects.Select(aspect => (IInterceptor)provider.GetRequiredService(implementationDictionary[aspect])).ToArray();

                        return generator.CreateInterfaceProxyWithTarget(serviceDescriptor.ServiceType, target: target, interceptors: aspectImplementations);
                    },
                    lifetime: serviceDescriptor.Lifetime);

                var index = services.IndexOf(serviceDescriptor);
                services.Insert(index, replacement);
                services.Remove(serviceDescriptor);
            }
            
            return services;
        }
    }
}
