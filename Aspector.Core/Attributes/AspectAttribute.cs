using Castle.DynamicProxy;

namespace Aspector.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public abstract class AspectAttribute : Attribute, IInterceptor
    {
        public abstract void Intercept(IInvocation invocation);
    }
}
