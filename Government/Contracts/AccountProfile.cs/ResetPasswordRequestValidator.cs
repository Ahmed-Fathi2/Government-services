using Government.Contracts.AccountProfile.cs;
using SurvayBasket.Abstractions.Consts.cs;

namespace SurvayBasket.Contracts.AccountProfile.cs
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordRequestValidator()
        {


            RuleFor(x => x.NewPassword)
             .NotEmpty()
             .Matches(RegexPattern.passwordPattern)
             .MinimumLength(8);


            RuleFor(x => x.Email)
              .NotEmpty()
              .EmailAddress();

            RuleFor(x => x.ResetToken)
              .NotEmpty();


        }
    }
}