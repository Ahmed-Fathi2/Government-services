namespace SurvayBasket.Contracts.AccountProfile.cs
{
    public record UserProfileResponse
    (
        string id,
        string FirstName ,
        string LastName ,
        string Email ,
        string UserName,
        string PhoneNumber,
       IList<string> Roles

    );
}
