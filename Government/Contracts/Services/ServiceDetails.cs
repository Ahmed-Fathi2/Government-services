using Government.Contracts.FilesAndFileds;
using Government.Contracts.Services;

namespace Government.Contracts.Services
{
    public record ServiceDetails
     (
        int Id,
        string ServiceName,
        string ServiceDescription,
        string Category,
        decimal Fee,
        string ProcessingTime,
        string ContactInfo,
        List<FileDetails> RequiredFiles
        );
}

