using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspector.Core.Services
{
    public class DecoratorServices : IDecoratorServices
    {
        private readonly ILoggerFactory _loggerFactory;
        
        public DecoratorServices(ILoggerFactory loggerFactory, IHostApplicationLifetime hostLifetime)
        {
            _loggerFactory = loggerFactory;
            GlobalToken = hostLifetime.ApplicationStopping;
        }

        public CancellationToken GlobalToken { get; }

        public void AddProvider(ILoggerProvider provider) => _loggerFactory.AddProvider(provider);
        public ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);
        public void Dispose() => _loggerFactory.Dispose();
    }
}
