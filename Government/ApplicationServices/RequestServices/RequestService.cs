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
using MassTransit;
using NotificationService.Models;
using static System.Net.WebRequestMethods;
namespace Government.ApplicationServices.RequestServices
{
    public class RequestService(AppDbContext context, IHttpContextAccessor httpContextAccessor,
         ILogger<RequestService> logger, IAttachedFileServcie attachedFileServcie,
         IPaymentService paymentService, IHttpClientFactory httpClientFactory,
          IPublishEndpoint publishEndpoint
       ) : IRequestService
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ILogger<RequestService> logger = logger;
        private readonly IAttachedFileServcie attachedFileServcie = attachedFileServcie;
        private readonly IPaymentService _paymentService = paymentService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IPublishEndpoint publish = publishEndpoint;

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
                    r.Member.FirstName.Contains(search) ||
                    r.Member.LastName!.Contains(search) ||
                    r.service.ServiceName.Contains(search) ||
                    r.Id.ToString()==(search));     
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


        public async Task<Result<SubmitResponseDto>> SubmitRequestAsync(SubmitRequestDto requestDto, CancellationToken cancellationToken)
        {

            var service = await _context.Services.FindAsync(requestDto.ServiceId);
            if (service == null)
                return Result.Falire<SubmitResponseDto>(ServiceError.ServiceNotFound);

            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var userInfo = await _context.Members.FindAsync(userId!, cancellationToken);
            if (userInfo == null)
            {
                var externalUser = await GetUserFromCentralDatabaseAsync(userId!, cancellationToken);
                if (externalUser == null)
                    return Result.Falire<SubmitResponseDto>(UsersErrors.NotFound);

                var newUser = new Member
                {
                    Id = externalUser.Id,
                    FirstName = externalUser.FirstName,
                    LastName = externalUser.LastName,
                    // باقي الخصائص حسب الحاجة
                };

                _context.Members.Add(newUser);
                await _context.SaveChangesAsync(cancellationToken);

                userInfo = newUser;
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var request = new Request
                {
                    RequestDate = DateTime.UtcNow,
                    MemberId = userId!,
                    ServiceId = requestDto.ServiceId
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

                var paymentResult = await _paymentService.MakeTransaction(
                    requestDto.PaymentMethodId,
                    request.Id,
                    service.Fee,
                    userId!,
                    $"{userInfo.FirstName} {userInfo.LastName}",
                    service.ServiceName,
                    cancellationToken);

                if (!paymentResult.IsSuccess)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result.Falire<SubmitResponseDto>(paymentResult.Error);
                }

                await attachedFileServcie.UploadManyAttachedAsync(requestDto.Files, request.Id, cancellationToken);


                var notification = new NotificationMessage
                {
                    Title = "✅ تم استلام طلبك بنجاح",
                    Body = $$"""
<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>تأكيد استلام الطلب</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700&display=swap');

        body {
            font-family: 'Tajawal', sans-serif;
            background-color: #f5f9ff;
            padding: 20px;
            line-height: 1.6;
            color: #333;
            margin: 0;
        }

        .email-container {
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }

        .header {
            background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
            color: white;
            text-align: center;
            padding: 30px 20px;
        }

        .header h1 {
            margin: 0;
            font-size: 22px;
            font-weight: 700;
        }

        .success-icon {
            font-size: 40px;
            margin-bottom: 15px;
            display: inline-block;
        }

        .content {
            padding: 30px;
        }

        .message-text {
            margin-bottom: 20px;
            font-size: 16px;
        }

        .service-name {
            color: #2563eb;
            font-weight: 700;
            background-color: #eff6ff;
            padding: 2px 6px;
            border-radius: 4px;
        }

        .status-box {
            background: #eff6ff;
            border-right: 4px solid #2563eb;
            padding: 15px;
            border-radius: 6px;
            margin: 25px 0;
            text-align: center;
            font-weight: 500;
        }

        .footer {
            background: #f1f5f9;
            text-align: center;
            padding: 20px;
            font-size: 14px;
            color: #64748b;
            border-top: 1px solid #e2e8f0;
        }

        .signature {
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px dashed #e2e8f0;
            font-style: italic;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <div class="success-icon">✓</div>
            <h1>تم استلام طلبك بنجاح</h1>
        </div>

        <div class="content">
            <p class="message-text">عزيزي العميل،</p>

            <p class="message-text">
                نود إعلامك بأنه تم استلام طلبك لخدمة 
                <span class="service-name" id="serviceNamePlaceholder">{{service.ServiceName}}</span>
                بنجاح وسيتم معالجته في أقرب وقت ممكن.
            </p>

            <div class="status-box">
                <span style="color: #2563eb;">حالة الطلب:</span> قيد المراجعة
            </div>

            <p class="message-text">
                سوف تتلقى إشعاراً عند اكتمال مراجعة الطلب. يمكنك تتبع حالة الطلب من خلال حسابك على المنصة.
            </p>

            <p class="message-text">
                لمزيد من المعلومات، لا تتردد في التواصل مع فريق الدعم لدينا.
            </p>

            <div class="signature">
                <strong>مع خالص التقدير،</strong><br>
                فريق الخدمات الحكومية
            </div>
        </div>

        <div class="footer">
            هذه رسالة آلية - يرجى عدم الرد عليها<br>
            © 2023 جميع الحقوق محفوظة لمنصتنا
        </div>
    </div>

    <script>
        // هذه القيمة للتجربة فقط، في التطبيق الحقيقي سيتم استبدالها من الخادم
        const serviceData = {
            ServiceName: "خدمة الدعم الفني الممتاز" // هذه قيمة افتراضية للتجربة
        };

        // الطريقة 1: إذا كان المتغير متاحًا في السياق الحالي
        if(typeof serviceData !== 'undefined' && serviceData.ServiceName) {
            document.getElementById('serviceNamePlaceholder').textContent = serviceData.ServiceName;
        }

        // الطريقة 2: جلب القيمة من معامل URL
        const urlParams = new URLSearchParams(window.location.search);
        const urlServiceName = urlParams.get('service');
        if(urlServiceName) {
            document.getElementById('serviceNamePlaceholder').textContent = decodeURIComponent(urlServiceName);
        }

        // الطريقة 3: جلب البيانات من API (مثال)
        async function fetchServiceName() {
            try {
                const response = await fetch('/api/get-service-details');
                const data = await response.json();
                if(data && data.ServiceName) {
                    document.getElementById('serviceNamePlaceholder').textContent = data.ServiceName;
                }
            } catch (error) {
                console.error('حدث خطأ أثناء جلب بيانات الخدمة:', error);
                // يمكنك وضع قيمة افتراضية في حالة الخطأ
                document.getElementById('serviceNamePlaceholder').textContent = "الخدمة المطلوبة";
            }
        }

        // في التطبيق الحقيقي، اختر إحدى الطرق التالية حسب احتياجك:
        // 1. إذا كانت البيانات تأتي من الخادم مباشرة في المتغير serviceData
        // 2. إذا كنت تريد جلبها من URL:
        // fetchServiceNameFromURL();
        // 3. إذا كنت تريد جلبها من API:
        // fetchServiceName();
    </script>
</body>
</html>
""",

                    Type = NotificationType.UserSpecific,

                    Channels = new() { ChannelType.Email },

                    TargetUsers = new() { userId! },

                    Category = NotificationCategory.Alert
                };


                await publish.Publish(notification, ctx =>
                {
                    ctx.SetRoutingKey("user.notification.created");
                });


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
        /*
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

        */

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







    
