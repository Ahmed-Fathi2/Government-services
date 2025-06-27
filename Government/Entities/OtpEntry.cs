namespace Government.Entities
{
    public class OtpEntry
    {
        public int Id { get; set; }   
        public string Email { get; set; } = default!; 
        public string HashedOtp { get; set; } = default!; 
        public DateTime Expiry { get; set; }   
    }
}
