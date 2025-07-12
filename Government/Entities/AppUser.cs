namespace Government.Entities
{
    public class AppUser : IdentityUser
    {
       
        public string FirstName { get; set; }= string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
       
        public ICollection<AdminResponse> AdminResponses { get; set; } = [];
        public ICollection<AdminImage> image { get; set; } = [];
        

    }
}
