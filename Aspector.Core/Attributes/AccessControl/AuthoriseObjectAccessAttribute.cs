namespace Aspector.Core.Attributes.AccessControl
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AuthoriseObjectAccessAttribute<TResult> : AspectAttribute<TResult>
    {
        public bool ThrowIfNoHttpClaims { get; set; } = false;
    }
}
