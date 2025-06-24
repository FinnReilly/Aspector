
using Aspector.Examples.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aspector.Examples.Data
{
    public class DataSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public DataSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var scopedContext = serviceScope.ServiceProvider.GetRequiredService<ExampleContext>();

            await scopedContext.Database.MigrateAsync(cancellationToken);

            await scopedContext.Set<Team>().ExecuteDeleteAsync(cancellationToken);
            await scopedContext.Set<TeamMembership>().ExecuteDeleteAsync(cancellationToken);
            await scopedContext.Set<Person>().ExecuteDeleteAsync(cancellationToken);

            var teamA = new Team
            {
                Name = "A",
            };
            var teamA1 = new Team
            {
                Name = "A1"
            };
            var teamB = new Team
            {
                Name = "B",
            };
            var teamCSuite = new Team
            {
                Name = "C Suite"
            };
            scopedContext.Set<Team>().AddRange(teamA, teamA1, teamB, teamCSuite);
            var bob = new Person
            {
                Name = "Bob"
            };
            var dave = new Person
            {
                Name = "Dave",
                Manager = bob,
            };
            var anousha = new Person
            {
                Name = "Anousha",
                Manager = bob,
            };
            var cyril = new Person
            {
                Name = "Cyril",
                Manager = dave
            };
            var susan = new Person
            {
                Name = "Susan",
                Manager = dave,
            };
            var hodor = new Person
            {
                Name = "Hodor",
                Manager = anousha
            };
            scopedContext.Set<Person>().AddRange(bob, dave, cyril, anousha, hodor, susan);

            await scopedContext.SaveChangesAsync(cancellationToken);

            scopedContext.Set<TeamMembership>().AddRange(
                new TeamMembership { TeamId = teamA1.Id, PersonId = hodor.Id },
                new TeamMembership { TeamId = teamA.Id, PersonId = susan.Id },
                new TeamMembership { TeamId = teamA.Id, PersonId = cyril.Id },
                new TeamMembership { TeamId = teamB.Id, PersonId = anousha.Id},
                new TeamMembership { TeamId = teamB.Id, PersonId = dave.Id},
                new TeamMembership { TeamId = teamCSuite.Id, PersonId = bob.Id});

            await scopedContext.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
