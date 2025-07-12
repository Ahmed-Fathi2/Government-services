
using Government.ApplicationServices.UploadServiceImage;

namespace Government.ApplicationServices.UploadAdminImage
{
    public class Adminimage(IWebHostEnvironment webHostEnvironment, AppDbContext context) :IAdminImage
    {
        private readonly AppDbContext _context = context;
        private readonly string _filesPath = $"{webHostEnvironment.WebRootPath}/uploads/AdminImages";
        public async Task<int> UploadAdminImageAsync(IFormFile file, string AdminId, CancellationToken cancellationToken = default!)
        {
            var uploadedFile = await SaveRequiredFiles(file, AdminId, cancellationToken);
            await _context.AddAsync(uploadedFile, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return uploadedFile.Id;
        }

       


        private async Task<AdminImage> SaveRequiredFiles(IFormFile file, string AdminId, CancellationToken cancellationToken = default)
        {

            if (!Directory.Exists(_filesPath))
            {
                Directory.CreateDirectory(_filesPath);
            }

            var uploadedImage = new AdminImage
            {
                ImageName = file.FileName,
                ContentType = file.ContentType,
                ImageExtension = Path.GetExtension(file.FileName),
                AdminId = AdminId
            };

            var path = Path.Combine(_filesPath, file.FileName);

            using var stream = File.Create(path);
            await file.CopyToAsync(stream, cancellationToken);

            return uploadedImage;
        }
    }
}
