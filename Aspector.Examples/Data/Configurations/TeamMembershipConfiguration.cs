using Aspector.Examples.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aspector.Examples.Data.Configurations
{
    public class TeamMembershipConfiguration : IEntityTypeConfiguration<TeamMembership>
    {
        public void Configure(EntityTypeBuilder<TeamMembership> builder)
        {
            builder.ToTable("TeamMemberships");

            builder.HasKey(tm => new {tm.TeamId, tm.PersonId});

            builder.HasOne(tm => tm.Team).WithMany(t => t.TeamMembers);
            builder.HasOne(tm => tm.Person).WithMany(p => p.Teams);
        }
    }
}
