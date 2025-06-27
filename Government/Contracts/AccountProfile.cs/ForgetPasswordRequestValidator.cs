namespace Government.Contracts.AccountProfile.cs
{
    public class ForgetPasswordRequestValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgetPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
