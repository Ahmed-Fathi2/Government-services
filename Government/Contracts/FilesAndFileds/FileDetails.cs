namespace Government.Contracts.FilesAndFileds
{
    public record FileDetails
    (
       int Id,
       //string RequiredFileName,
       string AttachedFileName,
       string ContentType,
       string FileExtension
    );
}
