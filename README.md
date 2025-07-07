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
Given its focus as a framework for using Aspect-Oriented programming within dependency injection, Aspector does not currently support the use of aspects on methods which do not appear an interface which is registered as a service type.  Using them on any methods which are _not_ present on an interface registered in your DI container is not in fact guaranteed to do anything at all.