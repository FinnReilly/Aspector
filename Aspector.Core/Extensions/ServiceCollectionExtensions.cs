using Aspector.Core.Attributes;
using Aspector.Core.Attributes.Caching;
using Aspector.Core.Decorators;
using Aspector.Core.Exceptions;
using Aspector.Core.Models.Config;
using Aspector.Core.Models.Registration;
using Aspector.Core.Services;
using Aspector.Core.Static;
using Aspector.Core.Validation;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Aspector.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static AspectorSettings _defaultSettings = new AspectorSettings
        {
            ValidateAspectUsage = true
        };

        public static IServiceCollection AddAspects(this IServiceCollection services, Action<AspectorSettings>? configurator = null)
        {
            configurator?.Invoke(_defaultSettings);

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
                                foreach (var decorator in decoratorTypes)
                                {
                                    usedAttributes.Add(decorator.AttributeType);
                                }

                                CachedReflection.AttributeSummariesByClass[t.ImplementationType] = new AspectAttributeSummary(
                                    decoratedMethods.Select(
                                        method => (
                                            Method: method,
                                            Aspects: method.GetCustomAttributes(typeof(AspectAttribute), inherit: true)
                                                .Where(a => usedAttributes.Contains(a.GetType()))
                                                .Cast<AspectAttribute>()
                                                .ToArray()))
                                    .ToArray());

                                return true;
                            }

                            return false;
                        })
                .ToHashSet();

            var baseDecoratorType = typeof(BaseDecorator<>);
            var assignableTypes = usedAttributes.Select(
                t => {
                    return baseDecoratorType.MakeGenericType([t]);
                });

            Type? constructedGenericVersionOfType = null;
            var (implementationDictionary, reverseImplementationDictionary) = allTypes.Aggregate(
                seed: (Implementation: new Dictionary<Type, Type>(), ReverseImplementation: new Dictionary<Type, Type>()),
                func: (dictionaries, t) =>
                    {
                        if (t.IsAbstract)
                        {
                            return dictionaries;
                        }

                        var typeIsNonConstructedGeneric = t.IsGenericType && !t.IsConstructedGenericType;

                        var matchingAssignableType = assignableTypes.Where(
                            aType =>
                            {
                                if (typeIsNonConstructedGeneric)
                                {
                                    var isAssignable = false;
                                    var analysisComplete = false;
                                    var attributeType = aType.GenericTypeArguments[0];
                                    if (!attributeType.IsConstructedGenericType)
                                    {
                                        return false;
                                    }

                                    var typeGenerationChecking = t;
                                    while (!analysisComplete)
                                    {
                                        if (typeGenerationChecking.BaseType == null)
                                        {
                                            analysisComplete = true;
                                            continue;
                                        }

                                        // recurse up the tree till we find a constructed generic type
                                        typeGenerationChecking = typeGenerationChecking.BaseType;
                                        if (!typeGenerationChecking.IsConstructedGenericType)
                                        {
                                            continue;
                                        }

                                        var candidateGenericParameterType = typeGenerationChecking.GenericTypeArguments
                                            .FirstOrDefault(arg => arg.IsConstructedGenericType && arg.IsAssignableTo(typeof(AspectAttribute)));

                                        if (candidateGenericParameterType != null
                                            && candidateGenericParameterType.GetGenericTypeDefinition() == attributeType.GetGenericTypeDefinition())
                                        {
                                            var genericArgumentsForCandidateAttribute = candidateGenericParameterType.GetGenericArguments();
                                            var genericArgumentsSetByAnalysedType = genericArgumentsForCandidateAttribute
                                                .Where(x => x.IsGenericParameter && x.DeclaringType == t)
                                                .ToList();

                                            if (!genericArgumentsSetByAnalysedType.Any())
                                            {
                                                isAssignable = false;
                                                analysisComplete = true;
                                                continue;
                                            }

                                            var genericArgumentsForAnalysedType = t.GetGenericArguments().Where(a => a.IsGenericTypeParameter).ToArray();
                                            var constructedGenericAttibuteTypeArguments = attributeType.GenericTypeArguments;
                                            var canConstructDecoratorWithAttributeTypeArgs = genericArgumentsForAnalysedType.Length <= constructedGenericAttibuteTypeArguments.Length;

                                            if (canConstructDecoratorWithAttributeTypeArgs && constructedGenericAttibuteTypeArguments.Length == genericArgumentsForCandidateAttribute.Length)
                                            {
                                                isAssignable = true;
                                                var typeArgs = new List<Type>();
                                                foreach (var arg in genericArgumentsForAnalysedType)
                                                {
                                                    if (!genericArgumentsForAnalysedType.Contains(arg))
                                                    {
                                                        continue;
                                                    }

                                                    for (var i = 0; i < constructedGenericAttibuteTypeArguments.Length; i++)
                                                    {
                                                        var genericParameter = genericArgumentsForCandidateAttribute[i];
                                                        var genericArgument = constructedGenericAttibuteTypeArguments[i];

                                                        if (genericParameter == arg)
                                                        {
                                                            typeArgs.Add(genericArgument);
                                                        }
                                                    }
                                                }

                                                try
                                                {
                                                    constructedGenericVersionOfType = t.MakeGenericType(typeArgs.ToArray());
                                                }
                                                catch
                                                {
                                                    isAssignable = false;
                                                    analysisComplete = true;
                                                    continue;
                                                }
                                            }
                                        }

                                        analysisComplete = true;
                                    }

                                    return isAssignable;
                                }

                                return t.IsAssignableTo(aType);
                            }).FirstOrDefault();

                        if (matchingAssignableType == null)
                        {
                            return dictionaries;
                        }

                        var aspectArgumentType = matchingAssignableType
                            .GetGenericArguments()
                            .First(arg => arg.IsAssignableTo(typeof(AspectAttribute)));

                        if (usedAttributes.Contains(aspectArgumentType))
                        {
                            if (dictionaries.Implementation.TryGetValue(aspectArgumentType, out var existingImplementation))
                            {
                                throw new InvalidOperationException(
                                    $"Only one aspect implementation can inherit from BaseAspectImplementation<{aspectArgumentType.FullName}>," +
                                    $" but both {existingImplementation.FullName} and {t.FullName} implement this abstract class");
                            }

                            var implementationToAdd = typeIsNonConstructedGeneric ?
                                constructedGenericVersionOfType!
                                : t;

                            dictionaries.Implementation.Add(aspectArgumentType, implementationToAdd);
                            dictionaries.ReverseImplementation.Add(implementationToAdd, aspectArgumentType);
                        }

                        return dictionaries;
                    });


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

            // add required implementations, as singleton by default
            foreach(var type in reverseImplementationDictionary.Keys)
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

            CachedReflection.DecoratorTypesByAspectAttribute = implementationDictionary;

            if (_defaultSettings.ValidateAspectUsage)
            {
                // add validators and validation hosted service to service collection
                services.AddHostedService<AspectValidatorsService>();
            }
            
            return services;
        }
    }
}
