namespace Government.Contracts.Request
{
    public record RequestQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }
        public string? RequestStatus { get; set; }
        public string? ResponseStatus { get; set; }

        public string SortBy { get; set; } = "RequestDate";
        public string SortDirection { get; set; } = "asc"; 
        public bool? onlyEditedAfterRejection { get; set; }
    }

}

