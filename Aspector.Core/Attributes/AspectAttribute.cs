namespace Aspector.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public abstract class AspectAttribute : Attribute
    {
    }
}
