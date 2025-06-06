using Aspector.Core.Decorators.AccessControl;

namespace Aspector.Core.Attributes.AccessControl
{
    /// <summary>
    /// Triggers the use of an implementation of <see cref="AuthoriseObjectAccessAsyncDecorator{TResult}"/>.  This
    /// is an abstract base class that requires a specific implementation to be written based on domain-specific authorisation
    /// logic.
    /// </summary>
    /// <typeparam name="TResult">The object type for which object level authorisation is being carried out</typeparam>
    public class AuthoriseObjectAccessAsyncAttribute<TResult> : AuthoriseObjectAccessAttribute<TResult>
    {
    }
}
