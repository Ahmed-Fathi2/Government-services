namespace Government.Contracts.Services
{
    public record ServiceRequest
    (
        string ServiceName,
        string ServiceDescription,
        decimal Fee,
        string ProcessingTime,
        string category,
        string ContactInfo,
        List<RequiredFiles> Files,
        List<ServiceFields> ServiceFields,
        IFormFile ServiceImage

    );
}
