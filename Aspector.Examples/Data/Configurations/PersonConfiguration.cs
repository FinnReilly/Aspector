using Aspector.Examples.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aspector.Examples.Data.Configurations
{
    public class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Persons");

            builder.HasKey(x => x.Id);
            
            builder.HasMany(x => x.DirectReports).WithOne(x => x.Manager).HasForeignKey(x => x.ManagerId);
            builder.HasMany(x => x.Teams).WithOne(x => x.Person).HasForeignKey(x => x.PersonId);
        }
    }
}
