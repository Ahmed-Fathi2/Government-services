using FluentValidation;

namespace Government.Contracts.Services
{
    public class ServiceRequestValidator:AbstractValidator<ServiceRequest>
    {
        private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".gif"];
        private const long _maxFileSizeInBytes = 5 * 1024 * 1024; // 5MB
        public ServiceRequestValidator()
        {
            RuleFor(x => x.ServiceName)
             .NotEmpty()
             .MaximumLength(250);


            RuleFor(x => x.ServiceDescription)
                .NotEmpty()
                .MaximumLength(1500);
                 

            RuleFor(x => x.Fee)
                .NotEmpty()
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Fee must be a positive number.");

            RuleFor(x => x.ProcessingTime)
                .NotEmpty()
                .MaximumLength(250);

            RuleFor(x => x.ContactInfo)
                .NotEmpty()
                .MaximumLength(1500);

            RuleFor(x => x.ServiceImage)
           .NotNull().WithMessage("Image is required.")
           .DependentRules(() =>
           {
               RuleFor(x => x.ServiceImage)
                   .Must(file => file.Length > 0)
                   .WithMessage("Uploaded image is empty.")
                   .Must(file => _allowedExtensions.Contains(
                       System.IO.Path.GetExtension(file.FileName).ToLower()))
                   .WithMessage("Only image files (.jpg, .jpeg, .png, .gif) are allowed.")
                   .Must(file => file.Length <= _maxFileSizeInBytes)
                   .WithMessage("Image size must not exceed 5 MB.");
           });



            RuleForEach(x => x.ServiceFields)
           .SetValidator(new ServiceFieldsValidator());


            RuleForEach(x => x.Files)
           .SetValidator(new RequiredFilesValidator());
        }

    }
}

