namespace Government.ApplicationServices.UploadAdminImage
{
    public interface IAdminImage
    {
        Task<int> UploadAdminImageAsync(IFormFile file, string AdminId, CancellationToken cancellationToken = default!);

    }
}
