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
                 .Include(r => r.service)        
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
        <div style='font-family: "Tajawal", sans-serif; color: #1e293b; line-height: 1.7; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='text-align: center; margin-bottom: 25px;'>
                <div style='font-size: 24px; font-weight: 700; color: #2563eb; margin-bottom: 15px;'>تمت الموافقة على طلبك</div>
                <div style='font-size: 40px; color: #16a34a;'>✓</div>
            </div>
            
            <div style='background: #f8fafc; border-radius: 12px; padding: 25px; margin: 20px 0; text-align: center;'>
                <p style='margin-bottom: 15px;'>تفاصيل الطلب:</p>
                <div style='display: inline-block; background: #eff6ff; color: #1e40af; padding: 8px 20px; border-radius: 6px; font-weight: 700; border-right: 4px solid #2563eb; margin-bottom: 15px;'>
                    {request.service.ServiceName}
                </div>
                
                <div style='margin-top: 20px;'>
                    <span style='font-weight: 600;'>حالة الطلب:</span>
                    <span style='display: inline-block; background: #16a34a; color: white; padding: 4px 15px; border-radius: 20px; font-size: 14px; margin-right: 8px;'>مكتمل</span>
                </div>
            </div>
            
            <div style='text-align: center; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e2e8f0; color: #64748b;'>
                <strong>فريق خدمة العملاء</strong><br>
                منصتنا الرقمية
            </div>
        </div>
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
        <div style='font-family: "Tajawal", sans-serif; color: #1e293b; line-height: 1.7; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='text-align: center; margin-bottom: 25px;'>
                <div style='font-size: 24px; font-weight: 700; color: #dc2626; margin-bottom: 15px;'>تم رفض طلبك</div>
                <div style='font-size: 40px; color: #dc2626;'>✗</div>
            </div>
            
            <div style='background: #f8fafc; border-radius: 12px; padding: 25px; margin: 20px 0; text-align: center;'>
                <p style='margin-bottom: 15px;'>تفاصيل الطلب:</p>
                <div style='display: inline-block; background: #eff6ff; color: #1e40af; padding: 8px 20px; border-radius: 6px; font-weight: 700; border-right: 4px solid #2563eb; margin-bottom: 15px;'>
                    {request.service.ServiceName}
                </div>
                
                <div style='margin-top: 15px; margin-bottom: 20px;'>
                    <span style='font-weight: 600;'>حالة الطلب:</span>
                    <span style='display: inline-block; background: #dc2626; color: white; padding: 4px 15px; border-radius: 20px; font-size: 14px; margin-right: 8px;'>مرفوض</span>
                </div>
                
                <div style='background: #fee2e2; padding: 15px; border-radius: 8px;'>
                    <div style='font-weight: 600; color: #b91c1c; margin-bottom: 10px;'>سبب الرفض:</div>
                    <div style='color: #7f1d1d;'>{adminReplyToREquest.ResponseText}</div>
                </div>
            </div>
            
            <div style='text-align: center; margin-top: 40px; padding-top: 20px; border-top: 1px solid #e2e8f0; color: #64748b;'>
                <strong>فريق خدمة العملاء</strong><br>
                منصتنا الرقمية
            </div>
        </div>
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


