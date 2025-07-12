using Government.Contracts;
using Government.Contracts.AccountProfile.cs;
using Government.Contracts.FilesAndFileds;
using SurvayBasket.Contracts.AccountProfile.cs;

namespace SurvayBasket.ApplicationServices.UserAccount
{
    public interface IAccountService
    {

        Task<Result<UserProfileResponse>> GetUserProfileAsync();
        Task<Result> UpdateUserProfileAsync(UserUpdatedProfileRequest Request);
        Task<Result> ChangeUserPassword(ChangePassWordRequest Request);
    
        Task<Result> GenerateAndSendAsync(string email, CancellationToken ct = default); // Forget Password
        Task<Result<VerifyResponse>> VerifyAsync(string email, string otp, CancellationToken ct = default);

        Task<Result> ResetUserPassword(string Email, string ResetToken, string NewPassword, CancellationToken ct = default);

        Task<Result<ImageUrl>> DownloadAdminImageAsync(CancellationToken cancellationToken = default);
        Task<Result> UpdateAdminImageAsync(NewImage image, CancellationToken cancellationToken = default);


    }
}
