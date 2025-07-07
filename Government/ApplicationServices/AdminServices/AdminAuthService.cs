using Government.Authentication;
using Government.Errors;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SurvayBasket.ApplicationServices.SendingEmail;
using SurvayBasket.Contracts.Authentication;
using SurvayBasket.Helper.cs;
using SurvayBasket.UsreErrors;
using System.Text;

namespace Government.ApplicationServices.AdminServices
{
    public class AdminAuthService(AppDbContext context, IAdminJwtProvider jwtProvider, UserManager<AppUser> userManager, ILogger<AdminAuthService> logger,
        IHttpContextAccessor httpContextAccessor, IEmailSender emailSender) : IAdminAuthService
    {
        private readonly AppDbContext _context = context;
        private readonly IAdminJwtProvider _jwtProvider = jwtProvider;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly ILogger<AdminAuthService> _logger = logger;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IEmailSender _emailSender = emailSender;

        public async Task<Result<AdminLoginResponse>> GetAdminTokenAsync(string Email, string Password, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(Email);

            if (user == null)
                return Result.Falire<AdminLoginResponse>(UserAdminErrors.IvalidCredential);

            var IsValidPassword = await _userManager.CheckPasswordAsync(user, Password);

            if (!IsValidPassword)
                return Result.Falire<AdminLoginResponse>(UserAdminErrors.IvalidCredential);

            var userRoles = await _userManager.GetRolesAsync(user);

            var userPermissions = (from r in _context.Roles
                                   join p in _context.RoleClaims
                                   on r.Id equals p.RoleId
                                   where userRoles.Contains(r.Name!)
                                   select p.ClaimValue)
                                   .Distinct()
                                   .ToList();

            // generate token
            (string token, int expiresIn) = _jwtProvider.GenerateToken(user!, userRoles, userPermissions);

            var adminLoginResponse = new AdminLoginResponse(user.Id, user.FirstName, user.LastName, user.Email!, token, expiresIn);

            return Result.Success(adminLoginResponse);

        }

        public async Task<Result> RegisterAsync(RegisterRequest registerRequest, CancellationToken cancellationToken = default)
        {
            var EmailIsExist = await _userManager.FindByEmailAsync(registerRequest.Email);

            if (EmailIsExist is not null)
            {
                return Result.Falire(UsersErrors.DublicatedEmail);

            }

            var user = registerRequest.Adapt<AppUser>();

            var result = await _userManager.CreateAsync(user, registerRequest.Password);

            // problem in creation
            if (!result.Succeeded)
            {
                var error = result.Errors.First();

                return Result.Falire(new Error(error.Code, error.Description));

            }


            // Generate verification code

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            _logger.LogInformation("Confirmation code : {code} ", code);

            // Send email 

            var origin = _httpContextAccessor.HttpContext?.Request.Headers.Origin;

            var emailBody = EmailBodyBuilder.GenerateEmailBody("EmailStyle", templateModel:
            new Dictionary<string, string>
            {

                    {"{{name}}",$"{user.FirstName} {user.LastName}" },
                    {"{{action_url}}", $"{origin}/Auth/Confirm-Email?userId={user.Id},code={code}" }

                });

            await _emailSender.SendEmailAsync(registerRequest.Email, "Government services Team", emailBody);


            return Result.Success();

        }

        public async Task<Result> ConfirmEmailAsync(ConfirmationEmailRequest ConfirmEmailRequest, CancellationToken cancellationToken = default)
        {

            var user = await _userManager.FindByIdAsync(ConfirmEmailRequest.Id);

            if (user is null)
            {
                return Result.Falire(UsersErrors.InvalidCode);

            }

            if (user.EmailConfirmed)
            {
                return Result.Falire(UsersErrors.EmailIsConfirmBefore);

            }

            var code = ConfirmEmailRequest.ConfirmationToken;

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException)
            {
                return Result.Falire(UsersErrors.InvalidCode);
            }



            var result = await _userManager.ConfirmEmailAsync(user, code);


            if (!result.Succeeded)
            {

                var error = result.Errors.First();

                return Result.Falire(new Error(error.Code, error.Description));

            }


          //  await _userManager.AddToRoleAsync(user, DefaultRole.Member);

            return Result.Success();

        }

        public async Task<Result> ResendConfirmEmailAsync(ResendConfirmationEmail ResendConfirmationEmailRequest, CancellationToken cancellationToken = default)
        {


            var user = await _userManager.FindByEmailAsync(ResendConfirmationEmailRequest.Email);

            if (user is null)
            {
                return Result.Success(); // 

            }

            if (user.EmailConfirmed)
            {
                return Result.Falire(UsersErrors.EmailIsConfirmBefore);

            }

            // Generate verification code

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            _logger.LogInformation("Confirmation code : {code} ", code);

            // Send email 
            var origin = _httpContextAccessor.HttpContext?.Request.Headers.Origin;

            var emailBody = EmailBodyBuilder.GenerateEmailBody("EmailStyle", templateModel:
            new Dictionary<string, string>
            {

                    {"{{name}}",$"{user.FirstName} {user.LastName}" },
                    {"{{action_url}}", $"{origin}/Auth/Confirm-Email?userId={user.Id},code={code}" }

                });

            await _emailSender.SendEmailAsync(ResendConfirmationEmailRequest.Email, "Government services Team", emailBody);

            return Result.Success();

        }
    }


}


