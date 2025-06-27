using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Org.BouncyCastle.Crypto.Generators;
using SurvayBasket.Contracts.AccountProfile.cs;
using SurvayBasket.Contracts.User.cs;
using SurvayBasket.Helper.cs;
using SurvayBasket.UsreErrors;
using System.Security.Claims;
using System.Security.Cryptography;
using BCrypt.Net;

using System.Text;
using Government.Contracts.AccountProfile.cs;

namespace SurvayBasket.ApplicationServices.UserAccount
{
    public class AccountService(IHttpContextAccessor httpContextAccessor ,UserManager<AppUser> userManager ,
                        ILogger<AccountService> logger,
                        AppDbContext context) : IAccountService
    {
        private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
        private readonly UserManager<AppUser> userManager = userManager;
        private readonly ILogger<AccountService> logger = logger;
        private readonly AppDbContext context = context;

        private const int OtpLength = 6;                     // طول الرمز
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10); // مدة الصلاحية

        public async Task<Result<UserProfileResponse>> GetUserProfileAsync()
        {
            var userId = httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

           // var userId = "e1a6f7c2-5547-42b5-8178-446937b57c8e";

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return Result.Falire<UserProfileResponse>(UsersErrors.NotFound);

            var Roles = await userManager.GetRolesAsync(user);

            var response = new UserProfileResponse(user.Id, user.FirstName, user.LastName, user.Email!, user.UserName!, user.PhoneNumber!, Roles);

            return Result.Success(response);

        }

        public async Task<Result> UpdateUserProfileAsync(UserUpdatedProfileRequest Request)
        {
            //var userId = "e1a6f7c2-5547-42b5-8178-446937b57c8e";

            var userId = httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            var EmailIsExist = await context.Users.AnyAsync(x => (x.Email == Request.Email) && (x.Id != userId));

            if (EmailIsExist)
                return Result.Falire<UserResponse>(UsersErrors.DublicatedEmail);


            var PhoneNumIsExist = await context.Users.AnyAsync(x => (x.PhoneNumber == Request.PhoneNumber) && (x.Id != userId));

            if (PhoneNumIsExist)
                return Result.Falire<UserResponse>(UsersErrors.DublicatedPhoneNumber);

            await userManager.Users.Where(x => x.Id == userId)
                                   .ExecuteUpdateAsync(x => x.SetProperty(fn => fn.FirstName, Request.FirstName)
                                                             .SetProperty(ln => ln.LastName, Request.LastName)
                                                             .SetProperty(e=>e.Email,Request.Email)
                                                             .SetProperty(ne=>ne.NormalizedEmail , Request.Email.ToUpper())
                                                             .SetProperty(u=>u.UserName, Request.Email)
                                                             .SetProperty(nu=>nu.NormalizedUserName, Request.Email.ToUpper())
                                                             .SetProperty(p=>p.PhoneNumber , Request.PhoneNumber)
                                                            );

            return Result.Success();
        }


        public async Task<Result> ChangeUserPassword(ChangePassWordRequest Request)
        {
            var userId = httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            var user = await userManager.FindByIdAsync(userId);

           var result =  await userManager.ChangePasswordAsync(user!, Request.CurrentPassword, Request.NewPassword);

            if (result.Succeeded)
                return Result.Success();

            var error = result.Errors.First();
            return Result.Falire(new Error(error.Code, error.Description));
        }

        public async Task<Result> GenerateAndSendAsync(string email, CancellationToken ct = default)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return Result.Success();              

            // 1) إنشاء رمز عشوائي
            var otp = RandomNumberGenerator
                        .GetInt32((int)Math.Pow(10, OtpLength - 1),
                                  (int)Math.Pow(10, OtpLength))
                        .ToString();

            // logger.LogInformation("OTP for {Email} is {Otp}", email, otp); // 🔐 لا تنس حذفه لاحقًا!
            Console.WriteLine($"[DEBUG] OTP for {email} is: {otp}");

            // 2) تشفيره
            var hash = BCrypt.Net.BCrypt.HashPassword(otp);


            // 3) احذف أية رموز قديمة لهذا الإيميل
            await context.OtpEntries
                     .Where(e => e.Email == email)
                     .ExecuteDeleteAsync(ct);

            // 4) خزّن السطر الجديد
            await context.OtpEntries.AddAsync(new OtpEntry
            {
                Email = email,
                HashedOtp = hash,
                Expiry = DateTime.UtcNow.Add(OtpTtl)
            }, ct);
            await context.SaveChangesAsync(ct);


            /*
            // 5) ✉️ نشر إشعار «إيميل» عبر MassTransit → RabbitMQ
            //var notification = new NotificationMessage
            //{
            //    Title = "رمز التحقق لاستعادة كلمة المرور",
            //    Body = $"رمزك هو: {otp}. صالح لمدة {OtpTtl.TotalMinutes} دقيقة.",
            //    Type = NotificationType.UserSpecific,
            //    Channels = new() { ChannelType.Email },
            //    TargetUsers = new() { user.Id! },              // أو ضع الإيميل داخل Body إذا كان الـ consumer يعتمد عليه
            //    Category = NotificationCategory.Alert
            //};

            //await _publish.Publish(notification, ctx =>
            //{
            //    ctx.SetRoutingKey("user.notification.created"); // نفس الـ routing-key المتفق عليه
            //});
            */

            return Result.Success();
        }


        public async Task<Result<VerifyResponse>> VerifyAsync(string email, string otp, CancellationToken ct = default)
        {
            // 1) جلب آخر رمز لم ينتهِ بعد
            var entry = await context.OtpEntries
                .Where(e => e.Email == email && e.Expiry > DateTime.UtcNow)
                .OrderByDescending(e => e.Expiry)
                .FirstOrDefaultAsync(ct);

            // 2) تحقق من التوافق
            if (entry is null || !(BCrypt.Net.BCrypt.Verify(otp, entry.HashedOtp)))
               return Result.Falire<VerifyResponse>(UsersErrors.InvalidOTP);

            // 3) حذف السطر (One-time use)
            context.OtpEntries.Remove(entry);
            await context.SaveChangesAsync(ct);

            // 4) توليد ResetPasswordToken رسمي من Identity
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return Result.Falire<VerifyResponse>(UsersErrors.NotFound);

            var PasswordResetToken = await userManager.GeneratePasswordResetTokenAsync(user);

            return Result.Success(new VerifyResponse(PasswordResetToken));
        }


        public async Task<Result> ResetUserPassword(string Email, string ResetToken, string NewPassword, CancellationToken ct = default)
        {

            var user = await userManager.FindByEmailAsync(Email);

            if (user is null)
                return Result.Falire(UsersErrors.NotFound);

            var result = await userManager.ResetPasswordAsync(user, ResetToken, NewPassword);

            if (!result.Succeeded) 
            {
                var error = result.Errors.First();
                return Result.Falire(new Error(error.Code,error.Description));

            }

            return Result.Success();


        }
    }
}



/*
        //public async Task<Result> ForgetUserPassword(ForgetPasswordRequest Request)
        //{
        //    var userId= httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    var user = await userManager.FindByEmailAsync(Request.Email);

        //    if(user is null)
        //        return Result.Success();

        //    var code = await userManager.GeneratePasswordResetTokenAsync(user);
        //    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));


        //    logger.LogInformation("Rest Password {Token} ", code);

        //    var origin = httpContextAccessor.HttpContext?.Request.Headers.Origin;

        //    var forgetPassEmailBody =  EmailBodyBuilder.GenerateEmailBody("ForgetPssword",


        //        new Dictionary<string, string>
        //        {

        //            {"{{name}}",$"{user.FirstName}" },
        //            {"{{action_url}}",$"{origin}/change-Password?Email={user.Email},code={code}" }
                  

        //        } );



        //    await emailSender.SendEmailAsync(user.Email!,"Survay Basket Team " ,forgetPassEmailBody);


        //    return Result.Success();


        //}

       */