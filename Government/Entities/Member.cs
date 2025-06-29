namespace Government.Entities
{
    public class Member
    {
        public string Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }= string.Empty;

        public ICollection<Request> Requests { get; set; } = [];
    }
}
