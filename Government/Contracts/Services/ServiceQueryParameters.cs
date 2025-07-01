   namespace Government.Contracts.Services
    {
        public class ServiceQueryParameters
        {
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;

            public string? ServiceName { get; set; } // search

            public string? serviceCategory { get; set; } // Filter

            public bool? IsAvailable { get; set; } // filter
        }
    }


