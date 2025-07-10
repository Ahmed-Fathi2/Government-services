
using Government.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Government.ApplicationServices.UploadFiles
{
    public class AttachedFileServcie(IWebHostEnvironment webHostEnvironment, AppDbContext context) : IAttachedFileServcie
    {
        private readonly AppDbContext _context = context;
        private readonly string _filesPath = $"{webHostEnvironment.WebRootPath}/uploads/AttachedFiles";
        public async Task<int> UploadAttachedAsync(IFormFile file, int RequestId, CancellationToken cancellationToken = default!)
        {
            var uploadedFile = await SaveAttachedFiles(file, RequestId,  cancellationToken);
            await _context.AddAsync(uploadedFile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFile.Id;
        }
        public async Task<IEnumerable<int>> UploadManyAttachedAsync(List<IFormFile> files, int RequestId,  CancellationToken cancellationToken = default)
        {
            List<AttachedDocument> uploadedFiles = [];

            foreach (var file in files)
            {
                var uploadedFile = await SaveAttachedFiles(file, RequestId, cancellationToken);
                uploadedFiles.Add(uploadedFile);
            }

            await _context.AddRangeAsync(uploadedFiles, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFiles.Select(x => x.Id).ToList();
        }


        private async Task<AttachedDocument> SaveAttachedFiles(IFormFile file, int RequestId,  CancellationToken cancellationToken = default)
        {
            // var randomFileName = Path.GetRandomFileName();

            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath); // إنشاء الفولدر إذا مش موجود
            }

            var uploadedFile = new AttachedDocument
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileExtension = Path.GetExtension(file.FileName),
                RequestId= RequestId
            };

            var path = Path.Combine(_filesPath, file.FileName);

            using var stream = File.Create(path);
            await file.CopyToAsync(stream, cancellationToken);

            return uploadedFile;

        }
    }
}
