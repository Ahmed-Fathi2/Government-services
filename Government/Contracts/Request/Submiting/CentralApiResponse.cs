namespace Government.Contracts.Request.Submiting
{
    public class CentralApiResponse
    {
        public CentralUser Value { get; set; } = default!;
        public bool IsSuccess { get; set; }
        public bool IsFailure { get; set; }
    }
    public class CentralUser
    {
        public string Id { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Address { get; set; } = default!;
        public string BuildingNumber { get; set; } = default!;
        public string FloorNumber { get; set; } = default!;
    }
}
