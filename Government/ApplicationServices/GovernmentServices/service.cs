using Government.ApplicationServices.UploadFiles;
using Government.ApplicationServices.UploadServiceImage;
using Government.Contracts.Services;
using Government.Errors;
using Mapster;


namespace Government.ApplicationServices.GovernmentServices
{

    public class service(AppDbContext context, IRequiredFileServcie fileServcie, ILogger<service> logger, Iserviceimage iserviceimage) : IService
    {
        private readonly AppDbContext _context = context;
        private readonly IRequiredFileServcie _fileServcie = fileServcie;
        private readonly Iserviceimage _iserviceimage = iserviceimage;

        public async Task<Result<IEnumerable<ServiceResponse>>> GetAllAvailableServicesAsync(ServiceSearch serviceSearch, CancellationToken cancellationToken = default)
        {

            var query = _context.Services
                         .Where(x => x.IsAvailable)
                         .AsNoTracking()
                         .AsQueryable();

            if (!string.IsNullOrWhiteSpace(serviceSearch.ServiceName))
            {
                var search = serviceSearch.ServiceName.Trim();

                query = query.Where(r =>
                    r.ServiceName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(serviceSearch.serviceCategory))
            {
                query = query.Where(r => 
                    r.category == serviceSearch.serviceCategory);

            }

            var result = query
                         .ProjectToType<ServiceResponse>()
                         .AsNoTracking();

            return Result.Success<IEnumerable<ServiceResponse>>(result);

        }

        public async Task<Result<IEnumerable<ServiceResponse>>> GetAllServicesAsync(CancellationToken cancellationToken = default)
        {
            var services = await _context.Services
                                    .AsNoTracking()
                                    .ToListAsync(cancellationToken);

            var serviceResponse = services.Adapt<IEnumerable<ServiceResponse>>();

            return Result.Success(serviceResponse);
        }

        public async Task<Result<ServiceDetails>> GetServicesByIdAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            var Specificservice = await _context.Services
                      .Include(x => x.RequiredDocuments)
                      .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

            if (Specificservice is null)
                return Result.Falire<ServiceDetails>(ServiceError.ServiceNotFound);

            var serviceResponse = Specificservice.Adapt<ServiceDetails>();

            return Result.Success(serviceResponse)!;


        }
        
        public async Task<Result<ServiceResponse>> AddServiceAsync(ServiceRequest request, CancellationToken cancellationToken = default)
        {
            var isDuplicate = await _context.Services
                .AnyAsync(x => x.ServiceName == request.ServiceName || x.ServiceDescription == request.ServiceDescription, cancellationToken);

            if (isDuplicate)
                return Result.Falire<ServiceResponse>(ServiceError.DuplicatingNameOrDescription);

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // add service details
                var newService = new Service
                {
                    ServiceName = request.ServiceName,
                    ServiceDescription = request.ServiceDescription,
                    Fee = request.Fee,
                    ProcessingTime = request.ProcessingTime,
                    ContactInfo = request.ContactInfo,
                    category = request.category
                };
                await _context.Services.AddAsync(newService, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                //add fields

                var serviceFields = new List<ServiceField>();

                foreach (var fieldRequest in request.ServiceFields)
                {

                    var existingField = await _context.Fields
                        .FirstOrDefaultAsync(f => f.FieldName == fieldRequest.FieldName, cancellationToken);

                    Field fieldEntity;

                    if (existingField is not null)
                    {
                        fieldEntity = existingField;
                    }
                    else
                    {
                        fieldEntity = new Field
                        {
                            FieldName = fieldRequest.FieldName,
                            Description = fieldRequest.Description,
                            HtmlType = fieldRequest.HtmlType
                        };

                        await _context.Fields.AddAsync(fieldEntity, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken); // To get the Id
                    }

                    serviceFields.Add(new ServiceField
                    {
                        ServiceId = newService.Id,
                        FieldId = fieldEntity.Id
                    });
                }

                await _context.ServicesField.AddRangeAsync(serviceFields, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                //add files

                var serviceFiles = request.Files.Select(fileRequest => new RequiredDocument
                {
                    FileName = fileRequest.FileName,
                    ContentType =  $"application/{fileRequest.FileType}",
                    FileExtension = $".{fileRequest.FileType}", 
                            ServiceId = newService.Id
                        }).ToList();

                await _context.RequiredDocuments.AddRangeAsync(serviceFiles, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);



                // add service image 
                await _iserviceimage.UploadAsync(request.ServiceImage, newService.Id);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var serviceResponse = newService.Adapt<ServiceResponse>();
                return Result.Success(serviceResponse);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error Within Adding New Service");

                return Result.Falire<ServiceResponse>(RequestErrors.RequestNotCompleted);
            }
        }
        

        /*
        public async Task<Result<ServiceResponse>> AddServiceAsync(ServiceRequest request, CancellationToken cancellationToken = default)
        {
            var isDuplicate = await _context.Services
                .AnyAsync(x => x.ServiceName == request.ServiceName || x.ServiceDescription == request.ServiceDescription, cancellationToken);

            if (isDuplicate)
                return Result.Falire<ServiceResponse>(ServiceError.DuplicatingNameOrDescription);

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // add service details
                var newService = new Service
                {
                    ServiceName = request.ServiceName,
                    ServiceDescription = request.ServiceDescription,
                    Fee = request.Fee,
                    ProcessingTime = request.ProcessingTime,
                    ContactInfo = request.ContactInfo,
                    category = request.category
                };

                await _context.Services.AddAsync(newService, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken); // ضروري هنا لحفظ الـ Id

                // Prepare fields - Query مرة واحدة
                var fieldNames = request.ServiceFields.Select(f => f.FieldName).ToList();
                var existingFields = await _context.Fields
                    .Where(f => fieldNames.Contains(f.FieldName))
                    .ToListAsync(cancellationToken);

                var serviceFields = new List<ServiceField>();

                foreach (var fieldRequest in request.ServiceFields)
                {
                    var existingField = existingFields
                        .FirstOrDefault(f => f.FieldName == fieldRequest.FieldName);

                    Field fieldEntity;

                    if (existingField is not null)
                    {
                        fieldEntity = existingField;
                    }
                    else
                    {
                        fieldEntity = new Field
                        {
                            FieldName = fieldRequest.FieldName,
                            Description = fieldRequest.Description,
                            HtmlType = fieldRequest.HtmlType
                        };

                        await _context.Fields.AddAsync(fieldEntity, cancellationToken);
                        existingFields.Add(fieldEntity); // نضيفه للذاكرة لو تكرر الاسم
                    }

                    serviceFields.Add(new ServiceField
                    {
                        ServiceId = newService.Id,
                        FieldId = fieldEntity.Id
                    });
                }

                await _context.ServicesField.AddRangeAsync(serviceFields, cancellationToken);

                // رفع الملفات المطلوبة بشكل متوازي
                if (request.Files?.Any() == true)
                {
                    var fileUploadTasks = request.Files
                        .Select(file => _fileServcie.UploadAsync(file, newService.Id, cancellationToken));

                    await Task.WhenAll(fileUploadTasks);
                }

                // رفع صورة الخدمة
                if (request.ServiceImage != null)
                {
                    await _iserviceimage.UploadAsync(request.ServiceImage, newService.Id, cancellationToken);
                }

                // Save كل التغييرات مرة واحدة
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var serviceResponse = newService.Adapt<ServiceResponse>();
                return Result.Success(serviceResponse);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error Within Adding New Service");

                return Result.Falire<ServiceResponse>(RequestErrors.RequestNotCompleted);
            }
        }

        */
        public async Task<Result> ToggleServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            var service = await _context.Services
                                 .SingleOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

            if (service is null)
                return Result.Falire(ServiceError.ServiceNotFound);


            service.IsAvailable = !(service.IsAvailable);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();

        }

        public async Task<Result> UpdateServiceDetailsAsync(int serviceId, ServcieDescription request, CancellationToken cancellationToken = default)
        {
            var service = await _context.Services.SingleOrDefaultAsync(x => x.Id == serviceId, cancellationToken); // check service id 

            if (service is null)
                return Result.Falire(ServiceError.ServiceNotFound);

            var isDuplicate = await _context.Services
                                 .AnyAsync(x => (x.ServiceName == request.ServiceName || x.ServiceDescription == request.ServiceDescription)
                                        && x.Id != serviceId, cancellationToken);

            if (isDuplicate)
                return Result.Falire(ServiceError.DuplicatingNameOrDescription);

            request.Adapt(service);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();


        }

        public async Task<Result<IEnumerable<serviceCategoryResponse>>> GetAllserviceCategoryAsync(CancellationToken cancellationToken = default)
        {
            var Categories = await _context.Services
                        .Where(x => x.IsAvailable)
                        .Select(x => new serviceCategoryResponse(x.category))
                        .Distinct()
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);


            return Result.Success<IEnumerable<serviceCategoryResponse>>(Categories);


        }
    }
    
}
