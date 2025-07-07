<h1 style="display:flex; flex-direction: row;">
    <img src="a_spectre.svg" width="150" title="wooooooooo!"/>
    <div style="bottom:0px; display:flex; flex-direction: column; justify-content: end;">
        <h1 style="border:none; margin-bottom: 0px; padding-bottom:0px;">Aspector</h1>
        <h4 style="border:none; margin-top: 0px; padding-bottom:0px;">A spookily extensible AOP framework for Dependency Injection</h4>
    </div>
</h1>

Aspector leverages the object proxy pattern to provide Aspect-Oriented programming that integrates with native Dependency Injection in .NET 8.  It provides a number of pre-programmed "Aspects" such as caching and parameter logging (among others),  as well as a range of utility base classes to allow you to painlessly implement your own.

### Getting Started

To use in your project, simply decorate a class method which implements an interface method, for a service which is registered in your Dependency Injection container.  For example, to add caching to a method on a `PersonService`:

`````````````
[CacheResultAsync<Person>(timeToCacheSeconds: 10, slidingExpiration: false)]
public async Task<Person> GetPerson()
{
    ...
}
`````````````

> [!IMPORTANT]
>
> The method you decorate must be present on an interface which is registered in your dependency injection container.  Other methods will not be affected by the use of these attributes.
>
> This also means that calls to a decorated method from within the same class will not (currently) be affected.
> 
> Aspector is effective for decorating incoming method calls made from other dependencies


Next, in `Program` add **Aspector**'s services to the Dependency Injection container, before the call to `WebApplicationBuilder.Build` but *after* registering your application's services:

`````````````
builder.Services.AddSingleton<IPersonService, PersonService>();

builder.Services.AddAspects();

var app = builder.Build();
`````````````
You will need to call `AddAspects` even if only using your own Aspects.

### General Terms/Glossary

This document refers to certain elements of the library using specific terms whose meanings may not be obvious from their usage.  Here are the most important:

* *Aspect Attribute* - An attribute which Aspector uses to denote that an aspect is applied to a method.  These are used to pass arguments to the aspect's implementation, and must derive from the `AspectAttribute` class
* *Decorator* - The concrete implementation of an "Aspect" or cross-cutting concern as recognised by Aspector.  These will all ultimately derive from `BaseDecorator<TAspect>` where `TAspect` is an `AspectAttribute`
* *Aspect* - Used here to describe both the general concept of an Aspect as a cross-cutting concern and the unique pairing  of an *Aspect Attribute* with a *Decorator* which allows Aspector to detect which Aspect is being used

### Writing your own aspects

In order to write your own aspects, you must create two separate classes:

1) An Aspect Attribute class which derives from `AspectAttribute`
2) A Decorator class which derives from `BaseDecorator<TAspect>` (where `TAspect` is the attribute you just created)

Unlike your DI registered services, you do not need to register your decorator class.  `AddAspects` will register decorators as and when they are needed based on your usage of them.

> [!WARNING]
>
> Each aspect attribute type can be associated with only one decorator type or Aspector will throw an error.
>
> For instance, writing two decorators that both inherit from `BaseDecorator<MyAspectAttribute>` will cause your application to fail on startup.

Aspector offers a number of utility base classes to simplify the process of writing your own aspects.  The four most general of these are:

* `VoidDecorator<TAspect>` - designed for use on void methods or in cases where the return value of the method is of no concern.  For instance, the provided `LogDecorator` implementation is derived from `VoidDecorator<LogAttribute>`
* `ResultDecorator<TAspect, TResult>` - useful when your aspect needs to know the result of the targeted method. For instance, the provided `CacheResultDecorator<T>` implementation is derived from `ResultDecorator<CacheResultAttribute<TResult>, TResult>`
* `AsyncDecorator<TAspect>` - similar to the `VoidDecorator<TAspect>`, this is useful when the result of an awaitable task is of no concern, but it needs to be awaited for the decorator to work effectively.  For example, you may want to ensure that a scoped is closed only after a target method's returned `Task` has finished executing, as with the provided `AddLogPropertyAsyncDecorator` (derived from `AsyncDecorator<LogPropertyAttribute>`)
* `AsyncResultDecorator<TAspect, TResult>` - use this base type when your aspect needs to know the result of an awaitable task which is returned from a targeted method.

Using these four base classes will generally be a lot easier than directly using `BaseDecorator<TAspect>`.  First of all,  each provides an easily overridable `Decorate` method which will be called by Aspector's infrastructure when the target method is called.  This method will generally be passed a delegate representing the target method, a list of `TAspect` parameters from the current attribute layer, the current target method parameters, and key metadata about the target class and method contained within a `DecorationContext` object.

### Aspect Usage validation

Aspector offers you the option to build in your own validation of how aspects are used in a project.  There are two key ways to do this:

1) `ValidateUsageOrThrowAsync` - overriding this method on `BaseDecorator<TAspect>` allows you to validate the appropriateness of each aspect attribute (`TAspect`) in turn when applied to a method. A typical use case would be to check that a list of parameter names used in an attribute were actually present on the targeted method.  Throwing an error here will log an error message and shut down the application on startup
2) `ValidateUsagesAsync` - you can override this method from `BaseDecorator<TAspect>` in order to validate a group of attributes in a layer (a group of adjacent attributes of the same type - see below) on the same target method. Ensure that the `onException` delegate in this method is used if you want more than one error to appear in your logs here. As with `ValidateUsageOrThrowAsync`, throwing an exception from here will cause the application to shut down (as will calling `onException`)

### Some important concepts and limitations

#### Attribute Decorator Pairing

Each Aspect Attribute can only be the `TAspect` generic parameter for *one* Decorator.  Similar Decorators (for example `MyDecorator` and `MyDecoratorAsync`) must each have their own Aspect Attribute which denotes their use on a method.  Using an Aspect Attribute which has more than one matching Decorator will cause an error to be thrown on startup.

For practicality's sake, it is recommended that you give a paired Aspect Attribute and Decorator the same name, but suffixed respectively with `Attribute` and `Decorator`, eg. `MyAspectAttribute` and `MyAspectDecorator`.

#### Attribute Lifetime

By default, Decorators are registered as singletons.  This behaviour can be configured if you have a reason to do so, such as injecting scoped dependencies, by using the class-level `AspectLifetimeAttribute`.

*However, the library will not allow you to use a non-singleton aspect on any method of a singleton class, and will throw a `LifetimeMismatchException` if you do so.*

#### Layering

Aspects will be applied to your methods in the same order that they are added in code.  Where there are multiple consecutive attributes of the same type, these will be handled by the same Decorator (to avoid excessive layers of proxying).  Where are some attributes of type A, followed by one of type B and some more of type A, the two separate type A layers will be handled by different instances of the same Decorator.

Take this example:

````````````````````````
[AddLogProperty("IsFromCache", ConstantValue = true)]
[AddLogProperty("QueryType", ConstantValue = "Weather")]
[Log("Will check cache for result first")]
[CacheResult<IEnumerable<WeatherForecast>>(timeToCacheSeconds: 10, slidingExpiration: false)]
[Log("Cache must have expired, refreshing", LogLevel.Warning)]
public IEnumerable<WeatherForecast> GetWeather()
...
````````````````````````
Layering means that the second,  (warning) Log message will only be logged if the `CacheResult` aspect does *not* return a previously cached result, whereas the information log above will be logged for every method call.  Both of these log calls will be handled by a different instance of the `LogDecorator`, whereas the two uses of the `AddLogProperty` Aspect will be handled by the same instance of the `AddLogPropertyDecorator` class.


> *For this reason, you are strongly advised not to rely on instance state in your own Decorator implementations.  In larger applications it may become difficult to know which Decorator instance will be executing in which context and cause unexpected behaviour.  If you do need to use state in your Decorators, do so via a dependency.*

Another point to bear in mind with layering is that Decorator code which executes *after* the targeted method will execute in the reverse order that they are added in code.

#### Using attributes

Using attributes in any context in C# has its own set of limitations which it is useful to be aware of, such as:

* All constructor parameters must be constant values which are known at compile time
    * strings, numeric types or `Type` are all valid types
    * arrays of value types, strings, or `Type`s are also permitted (but other types of collection/`IEnumerable` are not)
    * array parameters can be represented as `params []` if required
    * other parameter types such as `DateTime` may need to be represented as strings
* Any attributes which take generic parameters must be used as _constructed_ generic types:
    * `[CacheResult<Person>(timeToCacheSeconds: 10)]` will work
    * `[CacheResult<T>(timeToCacheSeconds: 10)]` will cause a compiler error

#### Open Generics

If you wish to use Aspect Attributes in a generic class, this cannot then be registered as an open generic binding; this is not yet supported.  

Take, for example, the class in the Examples project of this repo - `Repository<TEntity>`.  Registering such a class like `services.AddScoped<IRepository<Person>, Repository<Person>>()` will work, while `services.AddScoped(typeof(IRepository<>), typeof(Repository<>))` will currently throw an error on startup.

#### Non-interface Methods
Given its focus as a framework for using Aspect-Oriented programming within dependency injection, Aspector does not currently support the use of aspects on methods which do not appear an interface that is registered as a service type.  Using them on any methods which are _not_ present on an interface registered in your DI container is not in fact guaranteed to do anything at all.

Additionally (as mentioned above) calls to a decorated method from within the same class will not be affected.