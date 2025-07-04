using Government.Contracts.Admin;
using Government.Contracts.Request;
using Government.Errors;
using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NotificationService.Models;
using System.Security.Claims;

namespace Government.ApplicationServices.AdminServices
{
    public class AdminResponseToRequest(AppDbContext context, IHttpContextAccessor httpContextAccessor, IPublishEndpoint publishEndpoint) : IAdminResponseToRequest
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IPublishEndpoint publishEndpoint = publishEndpoint;


       
        public async Task<Result<AdminReplyResult>> AddAdminResponseAsync(AdminReply adminReplyToREquest, CancellationToken cancellationToken = default)
        {

            //var request = await _context.Requests        
            //                    .FirstOrDefaultAsync(r=> r.Id==adminReplyToREquest.RequestId ,cancellationToken);
            var request = await _context.Requests
                 .Include(r => r.service)         // لجلب اسم الخدمة
                 .FirstOrDefaultAsync(r => r.Id == adminReplyToREquest.RequestId, cancellationToken);

            if (request is null)
                return Result.Falire<AdminReplyResult>(RequestErrors.RequestNotFound);



            var adminId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var adminResponse = new AdminResponse
            {
                RequestId = adminReplyToREquest.RequestId,
                UserId = adminId!,
                ResponseText = adminReplyToREquest.ResponseText,
                ResponseDate = DateTime.UtcNow
            };
            _context.AdminsResponse.Add(adminResponse);

            string notifTitle = string.Empty;
            string notifBody = string.Empty;


            if (adminReplyToREquest.Action == "Approve")
            {
                if (request.IsEditedAfterRejection)
                {
                    request.IsEditedAfterRejection = false;
                }

                request.RequestStatus = "Completed";
                request.ResponseStatus = "Responded";


                notifTitle = "🎉 تمت الموافقة على طلبك";
                notifBody = $"""
                      عزيزي المستخدم،

                      تمّت الموافقة على طلبك لخدمة "{request.service.ServiceName}".

                      يمكنك متابعة تفاصيل الطلب من خلال صفحة "طلباتي".
                      """;

            }
          
            else if (adminReplyToREquest.Action == "Reject")
            {
                if (request.IsEditedAfterRejection)
                {
                    request.IsEditedAfterRejection = false;
                }
                request.RequestStatus = "Rejected";
                request.ResponseStatus = "Responded";


                notifTitle = "❌ تم رفض طلبك";
                notifBody = $"""
                      عزيزي المستخدم،

                      تم رفض طلبك لخدمة "{request.service.ServiceName}" بسبب:
                      "{adminReplyToREquest.ResponseText}".

                      يمكنك تعديل الطلب ثم إعادة الإرسال :
                    
                      """;


            }

            await _context.SaveChangesAsync();

            var adminReplyResult = new AdminReplyResult(Message: "Response added and request updated successfully."
                                                      , RequestId: adminReplyToREquest.RequestId);
            var notification = new NotificationMessage
            {
                Title = notifTitle,
                Body = notifBody,
                Type = NotificationType.UserSpecific,
                Channels = new() { ChannelType.Email},
                TargetUsers = new() { request.MemberId },
                Category = NotificationCategory.Alert
            };

            await publishEndpoint.Publish(notification, ctx =>
            {
                ctx.SetRoutingKey("user.notification.created");
            }, cancellationToken);





            return Result.Success(adminReplyResult);




        }
        
       
    }
}


