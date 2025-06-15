using Aspector.Examples.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Aspector.Examples.Data
{
    public class ExampleContext : DbContext
    {
        public ExampleContext(DbContextOptions<ExampleContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TeamConfiguration());
            modelBuilder.ApplyConfiguration(new PersonConfiguration());
            modelBuilder.ApplyConfiguration(new TeamMembershipConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
