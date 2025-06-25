using Aspector.Examples.Models.Entities;

namespace Aspector.Examples.Services.Repository
{
    public interface IRepository<TEntity>
        where TEntity : class, IHasId
    {
        public Task<TEntity?> GetAsync(int id);
        public Task<TEntity?> GetCachedAsync(int id);
    }
}
