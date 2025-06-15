namespace Aspector.Examples.Models.Entities
{
    public class TeamMembership
    {
        public int TeamId { get; set; }
        public int PersonId { get; set; }

        public Team Team {  get; set; }
        public Person Person { get; set; }
    }
}
