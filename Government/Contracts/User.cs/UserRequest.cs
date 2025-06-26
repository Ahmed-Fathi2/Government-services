namespace SurvayBasket.Contracts.User.cs
{
    public record UserRequest(

        string FirstName,
        string LastName,
        string Email,
        string Password,
        String PhoneNumber,
        IList<string> Roles

        );
}
