using SurvayBasket.Abstractions;

namespace SurvayBasket.UsreErrors
{
    public static class UsersErrors
    {

        public static readonly Error IvalidCredential = new("invalid user credential", "invalid user or password");

        public static readonly Error NotFound = new("User Not found", "user id is not correct");

        public static readonly Error DublicatedEmail = new("DublicatedEmail", "Email is already exist");

        public static readonly Error DublicatedPhoneNumber = new("DublicatedPhoneNumber", "Phone Number is already exist");

        // related to login
        public static readonly Error NotConfirmedEamil = new("NotConfirmedEamil", "You must Confirm your Email before login");

        public static readonly Error InvalidCode = new("User.InvalidCode", "InvalidCode");


        public static readonly Error EmailIsConfirmBefore = new("EmailIsConfirmeBefore", "this Email is confirmed before");
    }
}
