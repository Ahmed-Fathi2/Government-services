namespace SurvayBasket.Contracts.AccountProfile.cs
{
    public class UserUpdatedProfileRequestValidator:AbstractValidator<UserUpdatedProfileRequest>
    {

        public UserUpdatedProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .Length(3, 200);


            RuleFor(x => x.LastName)
               .NotEmpty()
               .Length(3, 200);

            RuleFor(x => x.Email)
              .NotEmpty()
              .EmailAddress();

            RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Length(11)
            .Matches(@"^\d{11}$")
            .WithMessage("Phone number must be exactly 11 digits.");


        }
    }
}
