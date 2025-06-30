using Government.ApplicationServices.UploadFiles;
using Government.ApplicationServices.UploadServiceImage;
using Government.Contracts;
using Government.Contracts.FilesAndFileds;
using Government.Contracts.Services;
using Government.Entities;
using Government.Errors;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Government.ApplicationServices.Files
{
    public class FileService(AppDbContext context, IWebHostEnvironment webHostEnvironment, IRequiredFileServcie requiredFileServcie,
        IAttachedFileServcie attachedFileServcie, Iserviceimage serviceimage) : IFileService
    {
        private readonly IRequiredFileServcie _requiredFileServcie = requiredFileServcie;
        private readonly IAttachedFileServcie _attachedFileServcie = attachedFileServcie;
        private readonly Iserviceimage _serviceimage = serviceimage;
        private readonly string _filesPath = $"{webHostEnvironment.WebRootPath}/uploads";
        private readonly AppDbContext _context = context;

        public async Task<Result<IEnumerable<FileDetails>>> GetAttachedFilesAsync(int RequestId, CancellationToken cancellationToken = default)
        {
            var file = await _context.Requests.FindAsync(RequestId);

            if (file is null)
                return Result.Falire<IEnumerable<FileDetails>>(RequestErrors.RequestNotFound);

            var Attachedfiles = await _context.AttachedDocuments
                             .Where(f => f.RequestId == RequestId)
                             .Select(x => new FileDetails(
                                 x.Id,
                                 x.FileName,
                                 //   Path.Combine($"{_filesPath}/RequiredFiles",x.FileName),
                                 x.ContentType,
                                 x.FileExtension
                                 ))
                             .AsNoTracking()
                             .ToListAsync(cancellationToken);


            return Result.Success<IEnumerable<FileDetails>>(Attachedfiles);
        }

        public async Task<Result> UpdateUserFilesAsync(int RequestId, UserFiles userFiles, CancellationToken cancellationToken = default)
        {
            var Request = await _context.Requests.FindAsync(RequestId);
            if (Request == null)
                return Result.Falire(RequestErrors.RequestNotFound);


            var existingFilesInDb = await _context.AttachedDocuments
                .Where(f => f.RequestId == RequestId)
                .ToListAsync();


            var filesToDelete = existingFilesInDb
                .Where(f => !userFiles.Files.Any(file => file.FileName == f.FileName))
                .ToList();


            foreach (var file in filesToDelete)
            {
                var fullPath = Path.Combine($"{_filesPath}/AttachedFiles", file.FileName);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                _context.AttachedDocuments.Remove(file);
            }

            List<IFormFile> files = new List<IFormFile>();

            foreach (var newFile in userFiles.Files)
            {

                var existingFileInDb = existingFilesInDb
                    .FirstOrDefault(f => f.FileName == newFile.FileName);

                if (existingFileInDb == null)
                {
                    files.Add(newFile);
                }
            }

            await _attachedFileServcie.UploadManyAttachedAsync(files, RequestId, cancellationToken);

            if (Request.RequestStatus == "Rejected")
                Request.IsEditedAfterRejection = true;

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<DownLoadResponse>> DownloadAttachedFileAsync(int FileId, CancellationToken cancellationToken = default)
        {

            var file = await _context.AttachedDocuments.FindAsync(FileId);

            if (file is null)
                return Result.Falire<DownLoadResponse>(ServiceError.FileNotFound);

            var path = Path.Combine($"{_filesPath}/AttachedFiles", file.FileName);

            MemoryStream memoryStream = new();
            using FileStream fileStream = new(path, FileMode.Open);
            fileStream.CopyTo(memoryStream);

            memoryStream.Position = 0;

            var response = new DownLoadResponse(memoryStream.ToArray(), file.ContentType, file.FileName);

            return Result.Success(response);
        }

        public async Task<Result<IEnumerable<DocumentsResponse>>> GetServiceFilesAsync(int serviceId, CancellationToken cancellationToken)
        {

            var service = await context.Services.SingleOrDefaultAsync(x => x.Id == serviceId, cancellationToken); // check service id 

            if (service is null)
                return Result.Falire<IEnumerable<DocumentsResponse>>(ServiceError.ServiceNotFound);

            var Documents = await context.RequiredDocuments
                             .Where(x => x.ServiceId == serviceId)
                             .ProjectToType<DocumentsResponse>()
                             .AsNoTracking()
                             .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<DocumentsResponse>>(Documents);

        }

        public async Task<Result> UpdateFilesAsync(int serviceId, FilesUpdated updatedFiles, CancellationToken cancellationToken = default)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
                return Result.Falire(ServiceError.ServiceNotFound);


            var existingFilesInDb = await _context.RequiredDocuments
                .Where(f => f.ServiceId == serviceId)
                .ToListAsync();


            var filesToDelete = existingFilesInDb
                .Where(f => !updatedFiles.NewFiles.Any(file => file.FileName == f.FileName))
                .ToList();


                _context.RequiredDocuments.RemoveRange(filesToDelete);

           

            var NewFiles = updatedFiles.NewFiles
           .Where(newFile => !existingFilesInDb.Any(f => f.FileName == newFile.FileName))
           .ToList();


            var serviceNewFiles = NewFiles.Select(fileRequest => new RequiredDocument
            {
                FileName = fileRequest.FileName,
                ContentType = $"application/{fileRequest.FileType}",
                FileExtension = $".{fileRequest.FileType}",
                ServiceId = serviceId
            }).ToList();

            await _context.RequiredDocuments.AddRangeAsync(serviceNewFiles, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<DownLoadResponse>> DownloadServiceFileAsync(int FileId, CancellationToken cancellationToken = default)
        {
            var file = await _context.RequiredDocuments.FindAsync(FileId);

            if (file is null)
                return Result.Falire<DownLoadResponse>(ServiceError.FileNotFound);

            var path = Path.Combine($"{_filesPath}/RequiredFiles", file.FileName);

            MemoryStream memoryStream = new();
            using FileStream fileStream = new(path, FileMode.Open);
            fileStream.CopyTo(memoryStream);

            memoryStream.Position = 0;

            var response = new DownLoadResponse(memoryStream.ToArray(), file.ContentType, file.FileName);

            return Result.Success(response);
        }


        public async Task<Result<Imagedetails>> GetServiceImageAsync(int serviceId, CancellationToken cancellationToken)
        {

            var service = await context.Services.SingleOrDefaultAsync(x => x.Id == serviceId, cancellationToken); // check service id 

            if (service is null)
                return Result.Falire<Imagedetails>(ServiceError.ServiceNotFound);

            var imageDetails = await context.ServiceImages
                             .Where(x => x.ServiceId == serviceId)
                             .ProjectToType<Imagedetails>()
                             .AsNoTracking()
                             .SingleOrDefaultAsync(cancellationToken);

            return Result.Success(imageDetails!);

        }



        public async Task<Result<DownLoadResponse>> DownloadServiceImageAsync(int ImageId, CancellationToken cancellationToken = default)
        {
            var file = await _context.ServiceImages.FindAsync(ImageId);

            if (file is null)
                return Result.Falire<DownLoadResponse>(ServiceError.FileNotFound);

            var path = Path.Combine($"{_filesPath}/ServiceImages", file.ImageName);

            MemoryStream memoryStream = new();
            using FileStream fileStream = new(path, FileMode.Open);
            fileStream.CopyTo(memoryStream);

            memoryStream.Position = 0;

            var response = new DownLoadResponse(memoryStream.ToArray(), file.ContentType, file.ImageName);

            return Result.Success(response);
        }


      
        public async Task<Result> UpdateImageAsync(int serviceId, NewImage image, CancellationToken cancellationToken = default)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
                return Result.Falire(ServiceError.ServiceNotFound);

            var existingImage = await _context.ServiceImages
                 .FirstOrDefaultAsync(img => img.ServiceId == serviceId, cancellationToken);

         
            if (existingImage != null)
            {
                var oldImagePath = Path.Combine(_filesPath, "ServiceImages", existingImage.ImageName);
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);

                _context.ServiceImages.Remove(existingImage);
            }

            await _serviceimage.UploadAsync(image.newImage, serviceId);
            await _context.SaveChangesAsync(cancellationToken);


            await _context.SaveChangesAsync();
            return Result.Success();
        }

       
        


    }
}
