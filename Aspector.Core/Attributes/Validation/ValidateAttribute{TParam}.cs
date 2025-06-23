namespace Aspector.Core.Attributes.Validation
{
    /// <summary>
    /// Perform a validation action on all named parameters which match the <see cref="TParam"/> type.
    /// 
    /// If none are named then perform validation on the first parameter with the given type
    /// </summary>
    /// <typeparam name="TParam">The type of parameter which is being validated</typeparam>
    public abstract class ValidateAttribute<TParam> : ValidateAttribute
    {
        /// <param name="parameterNames">A list of named <see cref="TParam"/> parameters to validate</param>
        public ValidateAttribute(params string[] parameterNames):
            base(parameterNames)
        {
        }
    }
}
