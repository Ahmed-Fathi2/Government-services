using Government.ApplicationServices.UploadFiles;

namespace Government.ApplicationServices.UploadServiceImage
{
    public class serviceimage(IWebHostEnvironment webHostEnvironment, AppDbContext context) : Iserviceimage
    {
        private readonly AppDbContext _context = context;
        private readonly string _filesPath = $"{webHostEnvironment.WebRootPath}/uploads/ServiceImages";
        public async Task<int> UploadAsync(IFormFile file, int ServiceId, CancellationToken cancellationToken = default!)
        {
            var uploadedFile = await SaveRequiredFiles(file, ServiceId, cancellationToken);
            await _context.AddAsync(uploadedFile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFile.Id;
        }

        //public async Task<IEnumerable<int>> UploadManyAsync(List<IFormFile> files, int ServiceId, CancellationToken cancellationToken = default)
        //{
        //    List<RequiredDocument> uploadedFiles = [];

        //    foreach (var file in files)
        //    {
        //        var uploadedFile = await SaveRequiredFiles(file, ServiceId, cancellationToken);
        //        uploadedFiles.Add(uploadedFile);
        //    }

        //    await _context.AddRangeAsync(uploadedFiles, cancellationToken);
        //    await _context.SaveChangesAsync(cancellationToken);

        //    return uploadedFiles.Select(x => x.Id).ToList();
        //}


        private async Task<ServiceImage> SaveRequiredFiles(IFormFile file, int ServiceId, CancellationToken cancellationToken = default)
        {
            // var randomFileName = Path.GetRandomFileName();

            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath); 
            }

            var uploadedImage = new ServiceImage
            {
                ImageName = file.FileName,
                ContentType = file.ContentType,
                ImageExtension = Path.GetExtension(file.FileName),
                ServiceId = ServiceId
            };

        var path = Path.Combine(_filesPath, file.FileName);

            using var stream = File.Create(path);
            await file.CopyToAsync(stream, cancellationToken);

            return uploadedImage;
        }
    }
}
