using Aspector.Core.Attributes.AccessControl;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Claims;

namespace Aspector.Core.AccessControl
{
    public abstract class AuthoriseObjectAccessDecorator<TResult> : ResultDecorator<AuthoriseObjectAccessAttribute<TResult>, TResult>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthoriseObjectAccessDecorator(
            ILoggerFactory loggerFactory,
            IHttpContextAccessor httpContextAccessor,
            int layerIndex) 
            : base(loggerFactory, layerIndex)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected sealed override TResult Decorate(
            Func<object[]?, TResult> targetMethod,
            object[]? parameters,
            (ParameterInfo[] ParameterMetadata, MethodInfo DecoratedMethod, Type DecoratedType) decorationContext,
            IEnumerable<AuthoriseObjectAccessAttribute<TResult>> aspectParameters)
        {
            var claims = _httpContextAccessor.HttpContext?.User;
            var logger = GetLogger(decorationContext.DecoratedType);

            var result = targetMethod(parameters);
            if (claims == null)
            {
                if (aspectParameters.Any(p => p.ThrowIfNoHttpClaims))
                {
                    ThrowForNoClaimsPrincipal();
                }
                else
                {
                    logger.LogDebug($"Unable to retrieve any HTTP claims, no object-level authorisation will take place here");
                }
            }
            else
            {
                AuthoriseOrThrow(claims, result);
            }

            return result;
        }

        protected abstract void AuthoriseOrThrow(ClaimsPrincipal principal, TResult restrictedObject);

        protected virtual void ThrowForNoClaimsPrincipal() => throw new InvalidOperationException($"Could not authorise access to {typeof(TResult).Name} due to missing HTTP claims");
    }
}
