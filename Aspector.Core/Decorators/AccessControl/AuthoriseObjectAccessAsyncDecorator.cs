using Aspector.Core.Attributes.AccessControl;
using Aspector.Core.Decorators;
using Aspector.Core.Models;
using Aspector.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Aspector.Core.Decorators.AccessControl
{
    public abstract class AuthoriseObjectAccessAsyncDecorator<TResult> : AsyncResultDecorator<AuthoriseObjectAccessAsyncAttribute<TResult>, TResult>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthoriseObjectAccessAsyncDecorator(
            IDecoratorServices services,
            IHttpContextAccessor httpContextAccessor,
            int layerIndex) 
            : base(services, layerIndex)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override sealed async Task<TResult> Decorate(
            Func<object[]?, Task<TResult>> targetMethod,
            object[]? parameters,
            DecorationContext context,
            IEnumerable<AuthoriseObjectAccessAsyncAttribute<TResult>> aspectParameters)
        {
            var claims = _httpContextAccessor.HttpContext?.User;
            var logger = GetLogger(context);

            var result = await targetMethod(parameters);
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
                await AuthoriseOrThrowAsync(claims, result);
            }

            return result;
        }

        protected abstract Task AuthoriseOrThrowAsync(ClaimsPrincipal principal, TResult restrictedObject);

        /// <summary>
        /// This method should always throw an exception when overridden.  It is marked <see langword="virtual"/> so that you can throw
        /// an exception of your choosing.  This will only be called if the attribute's ThrowIfNoHttpClaims property is set to <see langword="true"/>
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual void ThrowForNoClaimsPrincipal() => throw new InvalidOperationException($"Could not authorise access to {typeof(TResult).Name} due to missing HTTP claims");
    }
}
