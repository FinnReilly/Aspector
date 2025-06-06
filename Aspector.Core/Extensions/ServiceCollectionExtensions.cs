﻿using Aspector.Core.Attributes;
using Aspector.Core.Decorators;
using Aspector.Core.Exceptions;
using Aspector.Core.Models.Registration;
using Aspector.Core.Services;
using Aspector.Core.Static;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IDecoratorServices, DecoratorServices>();

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
                                .Where(m => m.CustomAttributes.Any(attr => attr.AttributeType.IsAssignableTo(typeof(AspectAttribute))));

                            if (decoratedMethods?.Any() != true)
                            {
                                return false;
                            }

                            var decoratorTypes = decoratedMethods!.SelectMany(m => m.CustomAttributes.Where(attr => attr.AttributeType.IsAssignableTo(typeof(AspectAttribute))));
                            
                            if (decoratorTypes.Any())
                            {
                                foreach(var decorator in decoratorTypes)
                                {
                                    usedAttributes.Add(decorator.AttributeType);
                                }

                                CachedReflection.AttributeSummariesByClass[t.ImplementationType] = new AspectAttributeSummary(
                                    decoratedMethods.Select(
                                        method => (
                                            Method: method,
                                            Aspects: method.GetCustomAttributes()
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
            var reverseImplementationDictionary = new Dictionary<Type, Type>();
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
                            reverseImplementationDictionary.Add(t, aspectArgumentType);
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
            var maxLayerIndex = CachedReflection.AttributeSummariesByClass.Aggregate(
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
            foreach(var type in requiredImplementationTypes)
            {
                var layersToAdd = maxLayerIndex[reverseImplementationDictionary[type]] + 1;

                var lifeTimeToCreate = type.GetCustomAttribute<AspectLifetimeAttribute>()?.Lifetime ?? ServiceLifetime.Singleton;
                
                Action<Type, int, Func<IServiceProvider, object?, object>> registrationFunction = lifeTimeToCreate switch
                {
                    ServiceLifetime.Scoped => (type, i, func) => services.AddKeyedScoped(type, i, func),
                    ServiceLifetime.Transient => (type, i, func) => services.AddKeyedTransient(type, i, func),
                    ServiceLifetime.Singleton => (type, i, func) => services.AddKeyedSingleton(type, i, func),
                    _ => (type, i, func) => services.AddKeyedSingleton(type, i, func),
                };

                for (var  i = 0; i < layersToAdd; i++)
                {
                    // inject service key into base decorator as layer index
                    registrationFunction(type, i, (provider, key) => ActivatorUtilities.CreateInstance(provider, type, (int)key!));
                }
            }

            foreach(var serviceDescriptor in applicableServicesInContainer)
            {
                var matchingAspectAttributeSummary = CachedReflection.AttributeSummariesByClass[serviceDescriptor.ImplementationType!];

                var replacement = ServiceDescriptor.Describe(
                    serviceDescriptor.ServiceType,
                    implementationFactory: (provider) =>
                    {
                        var generator = provider.GetRequiredService<ProxyGenerator>();
                        var target = ActivatorUtilities.CreateInstance(provider, serviceDescriptor.ImplementationType!);
                        var aspectImplementations = matchingAspectAttributeSummary.WrapOrderFromInnermost
                            .Select(
                                aspectOrderDescriptor => 
                                {
                                    var implementationType = implementationDictionary[aspectOrderDescriptor.AspectType];

                                    var lifetimeAttribute = implementationType.GetAttribute<AspectLifetimeAttribute>();
                                    var implementationLifetime = lifetimeAttribute?.Lifetime ?? ServiceLifetime.Singleton;

                                    if (serviceDescriptor.Lifetime == ServiceLifetime.Singleton
                                        && implementationLifetime != ServiceLifetime.Singleton)
                                    {
                                        throw new LifetimeMismatchException(
                                            serviceDescriptor.ImplementationType ?? serviceDescriptor.ServiceType,
                                            implementationType,
                                            implementationLifetime);
                                    }

                                    return (IInterceptor)provider
                                        .GetRequiredKeyedService(
                                            implementationDictionary[aspectOrderDescriptor.AspectType],
                                            aspectOrderDescriptor.LayerIndex); 
                                })
                            .Reverse()
                            .ToArray();

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
