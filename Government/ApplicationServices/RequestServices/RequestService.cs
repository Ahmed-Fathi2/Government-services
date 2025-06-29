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
using System.Net.Http;
using System.Text.Json;
using SurvayBasket.UsreErrors;
using System.Diagnostics;
namespace Government.ApplicationServices.RequestServices
{
    public class RequestService(AppDbContext context, IHttpContextAccessor httpContextAccessor,
         ILogger<RequestService> logger, IAttachedFileServcie attachedFileServcie, IPaymentService paymentService, IHttpClientFactory httpClientFactory
       ) : IRequestService
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ILogger<RequestService> logger = logger;
        private readonly IAttachedFileServcie attachedFileServcie = attachedFileServcie;
        private readonly IPaymentService _paymentService = paymentService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

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
                                          x.AdminResponse.OrderByDescending(x => x.ResponseDate).Select(x => x.ResponseText).FirstOrDefault() ?? "No Response"

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

        /*
                public async Task<Result<SubmitResponseDto>> SubmitRequestAsync(SubmitRequestDto requestDto, CancellationToken cancellationToken)
                {
                    var service = await _context.Services.FindAsync(requestDto.ServiceId);

                    if (service == null)
                        return Result.Falire<SubmitResponseDto>(ServiceError.ServiceNotFound);

                    var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                    var userinfo = await _context.Users.FindAsync(userId, cancellationToken);

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
                        var paymentResult = await _paymentService.MakeTransaction(request.Id, service.Fee, userId!, $"{userinfo.FirstName} {userinfo.LastName}", service.ServiceName, cancellationToken);
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

                */


        //public async Task<Result<SubmitResponseDto>> SubmitRequestAsync(SubmitRequestDto requestDto, CancellationToken cancellationToken)
        //{

        //    var service = await _context.Services.FindAsync(requestDto.ServiceId);
        //    if (service == null)
        //        return Result.Falire<SubmitResponseDto>(ServiceError.ServiceNotFound);

        //    var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        //    var userInfo = await _context.Members.FindAsync( userId! , cancellationToken);
        //    if (userInfo == null)
        //    {
        //        var externalUser = await GetUserFromCentralDatabaseAsync(userId!, cancellationToken);
        //        if (externalUser == null)
        //            return Result.Falire<SubmitResponseDto>(UsersErrors.NotFound);

        //        var newUser = new Member
        //        {
        //            Id = externalUser.Id,
        //            FirstName = externalUser.FirstName,
        //            LastName = externalUser.LastName,
        //            // باقي الخصائص حسب الحاجة
        //        };

        //        _context.Members.Add(newUser);
        //        await _context.SaveChangesAsync(cancellationToken);

        //        userInfo = newUser;
        //    }

        //    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        //    try
        //    {
        //        var request = new Request
        //        {
        //            RequestDate = DateTime.UtcNow,
        //            MemberId = userId!,
        //            ServiceId = requestDto.ServiceId
        //        };

        //        await _context.Requests.AddAsync(request, cancellationToken);
        //        await _context.SaveChangesAsync(cancellationToken);

        //        var serviceDataList = requestDto.ServiceData.Select(sd => new ServiceData
        //        {
        //            RequestId = request.Id,
        //            FieldId = sd.FieldId,
        //            FieldValueString = sd.FieldValueString,
        //            FieldValueInt = sd.FieldValueInt,
        //            FieldValueFloat = sd.FieldValueFloat,
        //            FieldValueDate = sd.FieldValueDate
        //        }).ToList();

        //        await _context.ServicesData.AddRangeAsync(serviceDataList, cancellationToken);
        //        await _context.SaveChangesAsync(cancellationToken);

        //        var paymentResult = await _paymentService.MakeTransaction(
        //            request.Id,
        //            service.Fee,
        //            userId!,
        //            $"{userInfo.FirstName} {userInfo.LastName}",
        //            service.ServiceName,
        //            cancellationToken);

        //        if (!paymentResult.IsSuccess)
        //        {
        //            await transaction.RollbackAsync(cancellationToken);
        //            return Result.Falire<SubmitResponseDto>(paymentResult.Error);
        //        }

        //        await attachedFileServcie.UploadManyAttachedAsync(requestDto.Files, request.Id, cancellationToken);

        //        await transaction.CommitAsync(cancellationToken);
        //        return Result.Success(new SubmitResponseDto(request.Id));
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync(cancellationToken);
        //        logger.LogError(ex, "Error submitting request with payment");
        //        return Result.Falire<SubmitResponseDto>(RequestErrors.RequestNotCompleted);
        //    }
        //}
        public async Task<Result<SubmitResponseDto>> SubmitRequestAsync(SubmitRequestDto requestDto, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            logger.LogInformation("Submitting request process started");

            stopwatch.Restart();
            // var service = await _context.Services.FindAsync(requestDto.ServiceId);
            var service = await _context.Services
     .AsNoTracking()
     .FirstOrDefaultAsync(s => s.Id == requestDto.ServiceId);

            stopwatch.Stop();
            logger.LogInformation($"⏱️ Time to fetch service from database: {stopwatch.ElapsedMilliseconds} ms");

            if (service == null)
                return Result.Falire<SubmitResponseDto>(ServiceError.ServiceNotFound);

            stopwatch.Restart();
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userInfo = await _context.Members.FindAsync(userId!, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation($"⏱️ Time to fetch user from local database: {stopwatch.ElapsedMilliseconds} ms");

            if (userInfo == null)
            {
                stopwatch.Restart();
                var externalUser = await GetUserFromCentralDatabaseAsync(userId!, cancellationToken);
                stopwatch.Stop();
                logger.LogInformation($"⏱️ Time to fetch user from Central API: {stopwatch.ElapsedMilliseconds} ms");

                if (externalUser == null)
                    return Result.Falire<SubmitResponseDto>(UsersErrors.NotFound);

                stopwatch.Restart();
                var newUser = new Member
                {
                    Id = externalUser.Id,
                    FirstName = externalUser.FirstName,
                    LastName = externalUser.LastName
                };

                _context.Members.Add(newUser);
                await _context.SaveChangesAsync(cancellationToken);
                stopwatch.Stop();
                logger.LogInformation($"⏱️ Time to add new user to local database: {stopwatch.ElapsedMilliseconds} ms");

                userInfo = newUser;
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                stopwatch.Restart();
                var request = new Request
                {
                    RequestDate = DateTime.UtcNow,
                    MemberId = userId!,
                    ServiceId = requestDto.ServiceId
                };

                await _context.Requests.AddAsync(request, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                stopwatch.Stop();
                logger.LogInformation($"⏱️ Time to create and save request: {stopwatch.ElapsedMilliseconds} ms");

                stopwatch.Restart();
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
                stopwatch.Stop();
                logger.LogInformation($"⏱️ Time to save service data fields: {stopwatch.ElapsedMilliseconds} ms");

                stopwatch.Restart();
                var paymentResult = await _paymentService.MakeTransaction(
                    request.Id,
                    service.Fee,
                    userId!,
                    $"{userInfo.FirstName} {userInfo.LastName}",
                    service.ServiceName,
                    cancellationToken);

                stopwatch.Stop();
                logger.LogInformation($"⏱️ Time to complete payment process: {stopwatch.ElapsedMilliseconds} ms");

                if (!paymentResult.IsSuccess)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Falire<SubmitResponseDto>(paymentResult.Error);
                }

                stopwatch.Restart();
                await attachedFileServcie.UploadManyAttachedAsync(requestDto.Files, request.Id, cancellationToken);
                stopwatch.Stop();
                logger.LogInformation($"⏱️ Time to upload attached files: {stopwatch.ElapsedMilliseconds} ms");

                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation($"✅ Request submitted successfully");

                return Result.Success(new SubmitResponseDto(request.Id));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "❌ Error occurred while submitting request");
                return Result.Falire<SubmitResponseDto>(RequestErrors.RequestNotCompleted);
            }
        }



        public async Task<UserDto?> GetUserFromCentralDatabaseAsync(string userId, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CentralApi");

                client.DefaultRequestHeaders.Remove("x-api-key");
                client.DefaultRequestHeaders.Add("x-api-key", "68adfd2c-714b-44b8-8469-7b183aaf51c5");

                var response = await client.GetAsync($"api/User/{userId}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var apiResponse = JsonSerializer.Deserialize<CentralApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse == null || apiResponse.IsFailure || apiResponse.Value == null)
                    return null;

                // تقسيم الاسم إلى أول اسم والباقي اسم العائلة
                var fullName = apiResponse.Value.FullName?.Trim() ?? string.Empty;
                var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                var firstName = nameParts.Length > 0 ? nameParts[0] : fullName;
                var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

                return new UserDto
                {
                    Id = apiResponse.Value.Id,
                    FirstName = firstName
                    //LastName = lastName,
                    //Email = apiResponse.Value.Email,
                    //PhoneNumber = apiResponse.Value.PhoneNumber
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch user from central database");
                return null;
            }
        }

    }
}







    
