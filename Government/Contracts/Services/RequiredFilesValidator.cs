namespace Government.Contracts.Services
{
    public class RequiredFilesValidator:AbstractValidator<RequiredFiles>
    {
        public RequiredFilesValidator()
        {
            RuleFor(x => x.FileName)
             .NotEmpty()
             .MaximumLength(500);


            RuleFor(x => x.FileType)
                .NotEmpty()
                .MaximumLength(20);
        }
    }
}
