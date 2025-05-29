using Microsoft.Extensions.Logging;

namespace Aspector.Core.Services
{
    public interface IDecoratorServices : ILoggerFactory
    {
        CancellationToken GlobalToken { get; }
    }
}
