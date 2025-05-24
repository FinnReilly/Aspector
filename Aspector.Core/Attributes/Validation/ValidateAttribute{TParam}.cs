namespace Aspector.Core.Attributes.Validation
{
    /// <summary>
    /// Perform a validation action on all named parameters which match the <see cref="TParam"/> type.
    /// 
    /// If none are named then perform validation on the first parameter with the given type
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    public class ValidateAttribute<TParam> : ValidateAttribute
    {
        public ValidateAttribute() { }
    }
}
