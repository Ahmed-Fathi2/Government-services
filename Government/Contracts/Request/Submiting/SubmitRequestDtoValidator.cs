using Government.Contracts.Request.Submiting;

namespace Government.Contracts.Request.Submiting
{
    public class SubmitRequestDtoValidator:AbstractValidator<SubmitRequestDto>
    {
        public SubmitRequestDtoValidator()
        {


            RuleFor(x => x.Files)
                .NotNull()
                 .Must(files => files != null && files.Any()).WithMessage("يجب إرفاق ملف واحد على الأقل")

                .ForEach(file => file
                    .Must(f => f.Length > 0).WithMessage("الملف لا يمكن أن يكون فارغًا")
                    .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("حجم الملف يجب ألا يتجاوز 5 ميجابايت")
                    .Must(f => IsValidFileType(f.FileName)).WithMessage("نوع الملف غير مدعوم (PDF، JPG، PNG فقط)")
                );

            RuleFor(x => x.ServiceData)
                .NotNull().WithMessage("حقول الخدمة مطلوبة")
                .Must(fields => fields != null && fields.Any()).WithMessage("يجب إدخال حقل خدمة واحد على الأقل");

            RuleForEach(x => x.ServiceData)
                .SetValidator(new ServiceDataDtoValidator());



            RuleFor(x => x.PaymentMethodId)
                .NotNull()
                 .WithMessage("يجب إرفاق اتمام عملية الدفع");
        }

                 private bool IsValidFileType(string fileName)
            {
                var validExtensions = new[] { ".pdf", ".jpg", ".png" };
                var extension = Path.GetExtension(fileName).ToLower();
                return validExtensions.Contains(extension);
            }
    }
}
