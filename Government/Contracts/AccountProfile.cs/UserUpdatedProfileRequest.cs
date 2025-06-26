namespace SurvayBasket.Contracts.AccountProfile.cs
{
    public record UserUpdatedProfileRequest
    (

        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber

    );
}
