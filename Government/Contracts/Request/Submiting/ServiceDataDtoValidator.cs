using Government.Contracts.Request.Submiting;
using System.Text.RegularExpressions;

namespace Government.Contracts.Request.Submiting
{
    public class ServiceDataDtoValidator :AbstractValidator<ServiceDataDto>
    {

        public ServiceDataDtoValidator()
        {
            RuleFor(x => x.FieldId)
               .GreaterThan(0);

            //RuleFor(x => x)
            //    .Must(x => x.FieldValueString != null || x.FieldValueInt.HasValue ||
            //              x.FieldValueFloat.HasValue || x.FieldValueDate.HasValue)
            //    .WithMessage("يجب إدخال قيمة واحدة على الأقل (نص، عدد، عشري، تاريخ)");

            RuleFor(x => x.FieldValueString)
                .MaximumLength(500)
                .When(x => x.FieldValueString != null)
                .WithMessage("القيمة النصية يجب ألا تتجاوز 500 حرف");

            //RuleFor(x => x.FieldValueString)
            //    .Matches(@"^(?=.*[a-zA-Z])[\p{L}\p{N}\p{P}\p{S}\s]+$")
            //    .WithMessage("يجب أن يحتوي النص على حرف واحد على الأقل، ويمكن أن يحتوي على أرقام وعلامات خاصة.")
            //    .When(x => !string.IsNullOrEmpty(x.FieldValueString));

            RuleFor(x => x.FieldValueInt)
                .GreaterThanOrEqualTo(0).When(x => x.FieldValueInt.HasValue)
                .WithMessage("القيمة العددية يجب ألا تكون سالبة");

            RuleFor(x => x.FieldValueFloat)
                .GreaterThanOrEqualTo(0).When(x => x.FieldValueFloat.HasValue)
                .WithMessage("القيمة العشرية يجب ألا تكون سالبة");

            RuleFor(x => x.FieldValueDate)
                .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.FieldValueDate.HasValue)
                .WithMessage("تاريخ الحقل يجب ألا يكون في المستقبل");

        }
      
    }
    }
/*
 RuleFor(x => x.FieldValueString)
    .Matches(@"^[\p{L}\s]+$") // يقبل فقط الحروف والمسافات
    .WithMessage("يجب أن يحتوي النص على حروف فقط بدون أرقام أو رموز خاصة.")
    .When(x => !string.IsNullOrEmpty(x.FieldValueString));

RuleFor(x => x.FieldValueInt)
    .NotNull().WithMessage("يجب إدخال قيمة رقمية.")
    .Must(value => value.ToString().All(char.IsDigit))
    .WithMessage("يجب أن يحتوي الحقل على أرقام فقط بدون حروف أو رموز.");

 */
