namespace Government.Contracts.AccountProfile.cs
{
    public record ResetPasswordDto(string Email, string ResetToken, string NewPassword);
}
