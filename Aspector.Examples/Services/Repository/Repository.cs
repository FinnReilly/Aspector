using Aspector.Core.Attributes.Caching;
using Aspector.Examples.Data;
using Aspector.Examples.Models.Entities;

namespace Aspector.Examples.Services.Repository
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IHasId
    {
        private readonly ExampleContext _exampleContext;

        public Repository(ExampleContext exampleContext)
        {
            _exampleContext = exampleContext;
        }

        public Task<TEntity?> GetAsync(int id)
        {
            return _exampleContext.FindAsync<TEntity>(id).AsTask();
        }

        [CacheResultAsync<object>(timeToCacheSeconds: 15)]
        public Task<TEntity?> GetCachedAsync(int id)
        {
            return GetAsync(id);
        }
    }
}
