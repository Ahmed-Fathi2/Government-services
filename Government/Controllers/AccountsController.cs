using Government.Abstractions;
using Government.Contracts.AccountProfile.cs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SurvayBasket.ApplicationServices.UserAccount;
using SurvayBasket.Contracts.AccountProfile.cs;
using SurvayBasket.UsreErrors;


namespace SurvayBasket.Controllers
{
    [Route("Account")]
    [ApiController]
    [Authorize]
    public class AccountsController(IAccountService accountService) : ControllerBase
    {
        private readonly IAccountService accountService = accountService;


        [HttpGet("User-Info")]
        public async Task<ActionResult> UserInfo()
        {

            var userInfo = await accountService.GetUserProfileAsync();

            return Ok(userInfo.Value());


        }


        [HttpPut("Update-Info")]
        public async Task<ActionResult> UpdateUserInfo(UserUpdatedProfileRequest request)
        {

            var result = await accountService.UpdateUserProfileAsync(request);

            return result.IsSuccess ?
                          NoContent()
                         :result.ToProblem(statuscode: StatusCodes.Status409Conflict);

        }


        [HttpPut("change-Password")]
        public async Task<ActionResult> ChangeUserPassword(ChangePassWordRequest request)
        {

            var userInfo = await accountService.ChangeUserPassword(request);

            return userInfo.IsSuccess ? NoContent() : userInfo.ToProblem(statuscode: StatusCodes.Status400BadRequest);


        }



        // 1) يطلب إرسال OTP
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto, CancellationToken ct)
        {
            await accountService.GenerateAndSendAsync(dto.Email, ct);
            return NoContent(); 
        }


        // 2) يتحقق من OTP
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto, CancellationToken ct)
        {
           
                var result = await accountService.VerifyAsync(dto.Email, dto.Otp, ct);
                
                if (result.IsSuccess)
                return Ok(result.Value());


            return result.Error.Equals(UsersErrors.InvalidOTP)
            ? result.ToProblem(statuscode: StatusCodes.Status400BadRequest)
            : result.ToProblem(statuscode: StatusCodes.Status404NotFound);// for user not found
           
        }

        // 3) يغيّر كلمة المرور
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto, CancellationToken ct)
        {
            var result = await accountService.ResetUserPassword(dto.Email, dto.ResetToken,dto.NewPassword, ct);

             return (result.IsSuccess)
                    ? NoContent() 
                    : result.ToProblem(statuscode: StatusCodes.Status400BadRequest);
        }

        //[HttpPost("Forget-Password")]
        //public async Task<ActionResult> ForgetUserPassword(ForgetPasswordRequest request)
        //{

        //    var userInfo = await accountService.ForgetUserPassword(request);

        //    return Ok();


        //}


        //[HttpPost("Reset-Password")]
        //public async Task<IActionResult> ResetUserPassword(ResetPasswordRequest Request)
        //{

        //    var result = await accountService.ResetUserPassword(Request);

        //    if (result.IsSuccess)
        //        return Ok();

        //    return result.Error.Equals(UsersErrors.InvalidCode) ?
        //                result.ToProblem(statuscode: StatusCodes.Status400BadRequest) 
        //              : result.ToProblem(statuscode: StatusCodes.Status401Unauthorized);


        //}

    }
}
