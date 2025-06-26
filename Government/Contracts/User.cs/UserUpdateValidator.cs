using SurvayBasket.Abstractions.Consts.cs;

namespace SurvayBasket.Contracts.User.cs
{
    public class UserUpdateValidator:AbstractValidator<UserUpdate>
    {
        public UserUpdateValidator()
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
