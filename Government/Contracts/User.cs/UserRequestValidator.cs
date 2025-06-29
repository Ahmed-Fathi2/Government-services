using SurvayBasket.Abstractions.Consts.cs;

namespace SurvayBasket.Contracts.User.cs
{
    public class UserRequestValidator:AbstractValidator<UserRequest>
    {

        public UserRequestValidator()
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

            RuleFor(x => x.Password)
             .NotEmpty()
             .Matches(RegexPattern.passwordPattern);


            RuleFor(x => x.Roles)
                .NotNull()
             .NotEmpty();


            RuleFor(x => x.Roles)
               .Must(x => x.Distinct().Count() == x.Count())
               .WithMessage("You Cant add dublicated Role For the same user")
               .When(x => x.Roles != null);







        }
    }
}
/*
     string id,
        string FirstName,
        string LastName,
        string Email,
        string Password,
        IEnumerable<string> Roles
 */