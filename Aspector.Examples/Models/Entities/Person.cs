namespace Aspector.Examples.Models.Entities
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ManagerId { get; set; }

        public ICollection<Person> DirectReports { get; set; } = new List<Person>();
        public Person? Manager { get; set; }
        public ICollection<TeamMembership> Teams { get; set; } = new List<TeamMembership>();
    }
}
