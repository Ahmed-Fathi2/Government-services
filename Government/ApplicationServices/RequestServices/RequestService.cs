using Government.ApplicationServices.UploadFiles;
using Government.Contracts.Request;
using Government.Contracts.Request.Submiting;
using Government.Entities;
using Government.Errors;
using Mapster;
using SurvayBasket.Abstractions;
using System.Linq;
using System.Security.Claims;
using System.Linq.Dynamic.Core;
using Government.ApplicationServices.PaymentService;
namespace Government.ApplicationServices.RequestServices
{
    public class RequestService(AppDbContext context, IHttpContextAccessor httpContextAccessor,
         ILogger<RequestService> logger, IAttachedFileServcie attachedFileServcie,IPaymentService paymentService
       ) : IRequestService
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ILogger<RequestService> logger = logger;
        private readonly IAttachedFileServcie attachedFileServcie = attachedFileServcie;
        private readonly IPaymentService _paymentService = paymentService;

        public async Task<Result<PaginationList<RequestsDetails>>> GetAllRequests(RequestQueryParameters parameters, CancellationToken cancellationToken)
        {
            var query = _context.Requests
                    .Include(r => r.Member)
                    .Include(r => r.service)
                    .AsQueryable();

            //  Search
            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var search = parameters.Search.Trim();

                query = query.Where(r =>
                    //r.Member.FirstName.Contains(search) ||
                    //r.Member.LastName.Contains(search) ||
                    r.Id.ToString().Contains(search));//||
                                                      // r.MemberId.Contains(search));     // 
            }
            //  Filter
            if (!string.IsNullOrEmpty(parameters.RequestStatus))
                query = query.Where(r => r.RequestStatus == parameters.RequestStatus);

            if (!string.IsNullOrEmpty(parameters.ResponseStatus))
                query = query.Where(r => r.ResponseStatus == parameters.ResponseStatus);

            //  Sorting
            if (!string.IsNullOrEmpty(parameters.SortBy))
            {
                query = query.OrderBy($"{parameters.SortBy} {parameters.SortDirection}");

            };
            if (parameters.onlyEditedAfterRejection == true)
            {
                query = query.Where(r => r.IsEditedAfterRejection == true);
            }


            //  Pagination
            var source = query
                    //.Include(r => r.Member)
                    //.Include(r => r.service)
                    .ProjectToType<RequestsDetails>()
                    .AsNoTracking();

            var response = await PaginationList<RequestsDetails>.CreateAsync(source, parameters.PageNumber, parameters.PageSize, cancellationToken);


            return Result.Success(response);
        }

        public async Task<Result<IEnumerable<RequestsDetailstoUser>>> GetAllUserRequests(CancellationToken cancellationToken)
        {

            var memberId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);


            var userRequests = await _context.Requests
                                 .Where(x => x.MemberId == memberId)
                                 .Select(x => new RequestsDetailstoUser(
                                     x.Id,
                                     x.ServiceId,
                                     x.service.ServiceName,
                                     x.RequestDate,
                                     x.RequestStatus,
                                     x.ResponseStatus,
                                     x.AdminResponse
                                         .OrderByDescending(r => r.ResponseDate)
                                         .Select(s => s.ResponseText)
                                         .FirstOrDefault() ?? "No Response Yet"
                                 ))
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<RequestsDetailstoUser>>(userRequests);

        }

        public async Task<Result<RequestDetailsResponse>> GetUserRequestAsync(int requestId, CancellationToken cancellationToken)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null)
                return Result.Falire<RequestDetailsResponse>(RequestErrors.RequestNotFound);

            var Request = await _context.Requests
                                  .Where(r => r.Id == requestId)
                                  .Select(x => new RequestDetailsResponse(
                                          x.Id,
                                          x.MemberId,
                                          x.service.ServiceName,
                                          x.RequestDate,
                                          x.RequestStatus,
                                          x.ResponseStatus,
                                          x.AdminResponse.OrderByDescending(x=>x.ResponseDate).Select(x=>x.ResponseText).FirstOrDefault() ?? "No Response"
                                         
                                          )
                                         ).AsNoTracking()
                                          .SingleOrDefaultAsync(cancellationToken);



          

            return Result.Success(Request)!;

        }

        public async Task<Result<IEnumerable<RequestsDetailstoUser>>> GetUserequestsByStatusAsync(string requestStatus, CancellationToken cancellationToken)
        {

            var memberId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.Requests.Where(r => r.MemberId == memberId && r.RequestStatus == requestStatus)
                        .Select(x => new RequestsDetailstoUser(
                            x.Id,
                            x.ServiceId,
                            x.service.ServiceName,
                            x.RequestDate,
                            x.RequestStatus,
                            x.ResponseStatus,
                            x.AdminResponse
                            .OrderByDescending(r => r.ResponseDate)
                            .Select(s => s.ResponseText)
                            .FirstOrDefault() ?? "No Response Yet"
                            )
                            )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

           // logger.LogInformation($"UserId: {UserId}, RequestStatus: {request}");


            return Result.Success<IEnumerable<RequestsDetailstoUser>>(requests);

        }
        /*
        public async Task<Result<SubmitResponseDto>> SubmitRequestAsync(SubmitRequestDto requestDto, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var request = new Request
                {
                    RequestDate = DateTime.UtcNow,
                    MemberId = userId!,
                    ServiceId = requestDto.ServiceId,
                    
                };
                await _context.Requests.AddAsync(request, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);



                var serviceDataList = requestDto.ServiceData.Select(sd => new ServiceData
                {
                    RequestId = request.Id,
                    FieldId = sd.FieldId,
                    FieldValueString = sd.FieldValueString,
                    FieldValueInt = sd.FieldValueInt,
                    FieldValueFloat = sd.FieldValueFloat,
                    FieldValueDate = sd.FieldValueDate
                }).ToList();
                await _context.ServicesData.AddRangeAsync(serviceDataList, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);


                await attachedFileServcie.UploadManyAttachedAsync(requestDto.Files,request.Id ,cancellationToken);
  
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(new SubmitResponseDto(request.Id));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error submitting request");

                return Result.Falire<SubmitResponseDto>(RequestErrors.RequestNotCompleted);
            }
        }
       
        */


        public async Task<Result<SubmitResponseDto>> SubmitRequestAsync(SubmitRequestDto requestDto, CancellationToken cancellationToken)
        {
            var service = await _context.Services.FindAsync(requestDto.ServiceId);

            if (service == null)
                return Result.Falire<SubmitResponseDto>(ServiceError.ServiceNotFound); 

            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            var  userinfo = await _context.Users.FindAsync(userId, cancellationToken);

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. إنشاء الطلب
                var request = new Request
                {
                    RequestDate = DateTime.UtcNow,
                    MemberId = userId!,
                    ServiceId = requestDto.ServiceId
                };

                await _context.Requests.AddAsync(request, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // 2. حفظ بيانات الحقول
                var serviceDataList = requestDto.ServiceData.Select(sd => new ServiceData
                {
                    RequestId = request.Id,
                    FieldId = sd.FieldId,
                    FieldValueString = sd.FieldValueString,
                    FieldValueInt = sd.FieldValueInt,
                    FieldValueFloat = sd.FieldValueFloat,
                    FieldValueDate = sd.FieldValueDate
                }).ToList();

                await _context.ServicesData.AddRangeAsync(serviceDataList, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // 3. تنفيذ الدفع
                //var paymentResult = await _paymentService.MakeTransaction(request.Id, requestDto.PaymentMethodId, cancellationToken);
                var paymentResult = await _paymentService.MakeTransaction(request.Id, service.Fee,userId! ,$"{userinfo.FirstName} {userinfo.LastName}" , service.ServiceName, cancellationToken);
                if (!paymentResult.IsSuccess)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Falire<SubmitResponseDto>(paymentResult.Error);
                }

                // 4. رفع الملفات
                await attachedFileServcie.UploadManyAttachedAsync(requestDto.Files, request.Id, cancellationToken);

                // 5. تأكيد المعاملة
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(new SubmitResponseDto(request.Id));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error submitting request with payment");
                return Result.Falire<SubmitResponseDto>(RequestErrors.RequestNotCompleted);
            }
        }


    }
}


////  public async Task<Result> UpdateRequestAsync(int requestId, IEnumerable<UpdateRequest> requestDto, CancellationToken cancellationToken)
//  {

//      var request = await _context.Requests.FindAsync(requestId);
//      if (request == null)
//          return Result.Falire<UpdateResponse>(RequestErrors.RequestNotFound);

//      foreach (var fieldDto in requestDto)
//      {
//          var fieldData = await _context.ServicesData.FindAsync(fieldDto.FieldDataId);
//          if (fieldData == null)
//              return Result.Falire<UpdateResponse>(RequestErrors.FieldDataNotFound);

//          var field = await _context.Fields.FindAsync(fieldDto.FieldId);
//          if (field == null)
//              return Result.Falire<UpdateResponse>(RequestErrors.FieldNotFound);

//          fieldDto.Adapt(fieldData);
//      }

//      await _context.SaveChangesAsync(cancellationToken);

//      return Result.Success();
//  }