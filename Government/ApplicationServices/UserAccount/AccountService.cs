using Mapster;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using SurvayBasket.Contracts.AccountProfile.cs;
using SurvayBasket.Contracts.User.cs;
using SurvayBasket.Helper.cs;
using SurvayBasket.UsreErrors;
using System.Security.Claims;
using System.Text;

namespace SurvayBasket.ApplicationServices.UserAccount
{
    public class AccountService(IHttpContextAccessor httpContextAccessor ,UserManager<AppUser> userManager ,
                        ILogger<AccountService> logger,IEmailSender emailSender,
                        AppDbContext context) : IAccountService
    {
        private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
        private readonly UserManager<AppUser> userManager = userManager;
        private readonly ILogger<AccountService> logger = logger;
        private readonly IEmailSender emailSender = emailSender;
        private readonly AppDbContext context = context;

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

        public async Task<Result> ForgetUserPassword(ForgetPasswordRequest Request)
        {
            var userId= httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = await userManager.FindByEmailAsync(Request.Email);

            if(user is null)
                return Result.Success();

            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));


            logger.LogInformation("Rest Password {Token} ", code);

            var origin = httpContextAccessor.HttpContext?.Request.Headers.Origin;

            var forgetPassEmailBody =  EmailBodyBuilder.GenerateEmailBody("ForgetPssword",


                new Dictionary<string, string>
                {

                    {"{{name}}",$"{user.FirstName}" },
                    {"{{action_url}}",$"{origin}/change-Password?Email={user.Email},code={code}" }
                  

                } );



            await emailSender.SendEmailAsync(user.Email!,"Survay Basket Team " ,forgetPassEmailBody);


            return Result.Success();


        }

        public async Task<Result> ResetUserPassword(ResetPasswordRequest Request)
        {


            var user = await userManager.FindByEmailAsync(Request.Email);

            if (user is null || !user.EmailConfirmed)
                return Result.Falire(UsersErrors.InvalidCode);

            var code = Request.Code;
            try
            {
                 code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Request.Code));
            }


            catch(FormatException) 
            {

                return Result.Falire(UsersErrors.InvalidCode);
            
            }



          

            var result =  await userManager.ResetPasswordAsync(user, code, Request.NewPassword);



            if (result.Succeeded)
                return Result.Success();

            var error = result.Errors.First();
            return Result.Falire(new Error(error.Code, error.Description));


        }
    }
}
