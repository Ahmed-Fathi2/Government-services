namespace Government.Entities
{
    public class AdminImage
    {
        public int Id { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ImageExtension { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string AdminId { get; set; } = default!;
        public AppUser appUser { get; set; } = default!;
    }
}
