using Government.Contracts;
using Government.Contracts.FilesAndFileds;

namespace Government.ApplicationServices.Files
{
    public interface IFileService
    {
        Task<Result<IEnumerable<FileDetails>>> GetAttachedFilesAsync(int RequestId, CancellationToken cancellationToken = default!);
        Task<Result> UpdateUserFilesAsync(int RequestId, UserFiles userFiles, CancellationToken cancellationToken = default!);
        Task<Result<IEnumerable<DocumentsResponse>>> GetServiceFilesAsync(int serviceId, CancellationToken cancellationToken);

        /*************/
        Task<Result> UpdateFilesAsync(int serviceId, FilesUpdated filesTest, CancellationToken cancellationToken = default!);
        Task<Result<DownLoadResponse>> DownloadServiceFileAsync(int FileId, CancellationToken cancellationToken = default!);
        Task<Result<DownLoadResponse>> DownloadAttachedFileAsync(int FileId, CancellationToken cancellationToken = default!);

        /***************/
        Task<Result<Imagedetails>> GetServiceImageAsync(int serviceId, CancellationToken cancellationToken);
        Task<Result<DownLoadResponse>> DownloadServiceImageAsync(int FileId, CancellationToken cancellationToken = default);
        Task<Result> UpdateImageAsync(int serviceId, NewImage image, CancellationToken cancellationToken = default);


    }
}
