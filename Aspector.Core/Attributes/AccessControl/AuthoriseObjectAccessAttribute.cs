using Aspector.Core.Decorators.AccessControl;

namespace Aspector.Core.Attributes.AccessControl
{
    /// <summary>
    /// Triggers the use of an implementation of <see cref="AuthoriseObjectAccessDecorator{TResult}"/>.  This
    /// is an abstract base class that requires a specific implementation to be written based on domain-specific authorisation
    /// logic.
    /// </summary>
    /// <typeparam name="TResult">The object type for which object level authorisation is being carried out</typeparam>

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AuthoriseObjectAccessAttribute<TResult> : AspectAttribute<TResult>
    {
        public bool ThrowIfNoHttpClaims { get; set; } = false;
    }
}
