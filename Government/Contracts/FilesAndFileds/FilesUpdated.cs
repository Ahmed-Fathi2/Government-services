using Government.Contracts.Services;

namespace Government.Contracts.FilesAndFileds
{
    public record FilesUpdated
    (
        List<RequiredFiles> NewFiles

    );
}
