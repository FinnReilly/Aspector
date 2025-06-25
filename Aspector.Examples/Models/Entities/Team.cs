namespace Aspector.Examples.Models.Entities
{
    public class Team : IHasId
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<TeamMembership> TeamMembers { get; set; } = new List<TeamMembership>();
    }
}
