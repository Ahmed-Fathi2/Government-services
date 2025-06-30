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
using MassTransit;
using NotificationService.Models;

namespace SurvayBasket.ApplicationServices.UserAccount
{
    public class AccountService(IHttpContextAccessor httpContextAccessor ,UserManager<AppUser> userManager ,
                        ILogger<AccountService> logger,
                        AppDbContext context, IPublishEndpoint publishEndpoint) : IAccountService
    {
        private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
        private readonly UserManager<AppUser> userManager = userManager;
        private readonly ILogger<AccountService> logger = logger;
        private readonly AppDbContext context = context;
        private readonly IPublishEndpoint publish = publishEndpoint;
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

          
            var otp = RandomNumberGenerator
                        .GetInt32((int)Math.Pow(10, OtpLength - 1),
                                  (int)Math.Pow(10, OtpLength))
                        .ToString();

     
            Console.WriteLine($"[DEBUG] OTP for {email} is: {otp}");
       
            var hash = BCrypt.Net.BCrypt.HashPassword(otp);
          
            await context.OtpEntries
                     .Where(e => e.Email == email)
                     .ExecuteDeleteAsync(ct);

         
            await context.OtpEntries.AddAsync(new OtpEntry
            {
                Email = email,
                HashedOtp = hash,
                Expiry = DateTime.UtcNow.Add(OtpTtl)
            }, ct);
            await context.SaveChangesAsync(ct);
         
            var notification = new NotificationMessage
            {
                Title = "رمز التحقق لاستعادة كلمة المرور",
                Body = $"رمزك هو: {otp}. صالح لمدة {OtpTtl.TotalMinutes} دقيقة.",
                Type = NotificationType.UserSpecific,
                Channels = new() { ChannelType.Email },
                TargetUsers = new() { "6a1bd8db-77e3-4dcf-a63d-583a853800d8" },
             // TargetUsers = new() {user.id!},
                Category = NotificationCategory.Alert
            };
            /*
            //var evt = new NotificationMessage
            //{
            //    Title = "رمز التحقق لاستعادة كلمة المرور",
            //    Body = $"رمزك هو: {otp}. صالح لمدة {OtpTtl.TotalMinutes} دقيقة.",
            //    Type = NotificationType.Group,
            //    Channels = new List<ChannelType> { ChannelType.Email },
            //    //TargetUsers = new List<string> { "g1623g6-12g31g-123g-123g-123g123g", "g1623g6-12g31g-123g-123g-123g123g" },
            //    TargetUsers = new List<string> { "b0069c9d-8115-43bc-9c73-f69eaa02bc28"},
            //    Category = NotificationCategory.Update
            //};
            */
            await publish.Publish(notification, ctx =>
            {
                ctx.SetRoutingKey("user.notification.created"); 
            });


            return Result.Success();
        }


        public async Task<Result<VerifyResponse>> VerifyAsync(string email, string otp, CancellationToken ct = default)
        {
           
            var entry = await context.OtpEntries
                .Where(e => e.Email == email && e.Expiry > DateTime.UtcNow)
                .OrderByDescending(e => e.Expiry)
                .FirstOrDefaultAsync(ct);

          
            if (entry is null || !(BCrypt.Net.BCrypt.Verify(otp, entry.HashedOtp)))
               return Result.Falire<VerifyResponse>(UsersErrors.InvalidOTP);

          
            context.OtpEntries.Remove(entry);
            await context.SaveChangesAsync(ct);

           
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