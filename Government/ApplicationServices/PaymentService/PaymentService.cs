using Government.Contracts.Payment;
using Government.Errors;
using Stripe;

namespace Government.ApplicationServices.PaymentService
{
    public class PaymentService(AppDbContext context) : IPaymentService
    {
        private readonly AppDbContext _context = context;
        private readonly PaymentIntentService _paymentIntentService = new();

        public async Task<Result<PaymentResponse>> MakeTransaction(int requestId, decimal ServcieCost,string userId,string userName,string serviceName, CancellationToken cancellationToken = default)
        {
            try
            {
                /***************************************************************************************************/
                /***************************************************************************************************/
                //  test instead of paymentMethod.Id form frontend
                var paymentMethodService = new PaymentMethodService();

                var paymentMethod = await paymentMethodService.CreateAsync(new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Token = "tok_visa" // يمكن تغييره لتجربة حالات أخرى
                    }
                });

                /***************************************************************************************************/
                /***************************************************************************************************/

                // 2. Create a PaymentIntent (authorization only)
                var intentCreateOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)ServcieCost * 100,
                    Currency = "egp",
                    PaymentMethod = paymentMethod.Id,
                    Confirm = true,
                    CaptureMethod = "manual",
                    Description = $"دفع طلب رقم {requestId}",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "RequestId", requestId.ToString() },
                        { "UserId", userId.ToString() }, 
                        { "UserName", userName },        
                        { "ServiceName", serviceName }  
                    }
                };


                var intent = await _paymentIntentService.CreateAsync(intentCreateOptions, cancellationToken: cancellationToken);

                if (intent.Status != "requires_capture")
                {
                    return Result.Falire<PaymentResponse>(RequestErrors.FailedPayment);
                }

                // 3. Capture the payment
                await _paymentIntentService.CaptureAsync(intent.Id);

                // 4. Get updated status after capture
                var updatedIntent = await _paymentIntentService.GetAsync(intent.Id);

                // 5. Save payment record
                var payment = new Payment
                {
                    Amount = ServcieCost ,
                    PaymentDate = DateTime.UtcNow,
                    PaymentStatus = updatedIntent.Status,
                    RequestId = requestId
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(cancellationToken);

                var response = new PaymentResponse(
                    payment.Id,
                    payment.Amount,
                    payment.PaymentDate,
                    payment.PaymentStatus);

                return Result.Success(response);
            }
            catch (StripeException ex)
            {
                var declineCode = ex.StripeError?.DeclineCode;

                if (declineCode == "insufficient_funds")
                    return Result.Falire<PaymentResponse>(RequestErrors.FailedPaymentNotenough);

                if (declineCode == "expired_card")
                    return Result.Falire<PaymentResponse>(RequestErrors.FailedPaymentExpiredCard);

                if (declineCode == "stolen_card")
                    return Result.Falire<PaymentResponse>(RequestErrors.FailedPaymentStolen);

                // fallback for any other failure
                return Result.Falire<PaymentResponse>(RequestErrors.FailedPayment);
            }
        }
    }
}

/*
 
| 🧪 **الحالة**       | 🔐 **Token (بديل آمن)**               | 💳 **رقم البطاقة (للاختبار المباشر)** | 📄 **الوصف**                         |
| ------------------- | ------------------------------------- | ------------------------------------- | ------------------------------------ |
| ✅ نجاح كامل         | `tok_visa`                            | `4242 4242 4242 4242`                 | عملية ناجحة بدون مشاكل               |
| ❌ لا يوجد رصيد كافٍ | `tok_chargeDeclinedInsufficientFunds` | `4000 0000 0000 9995`                 | رفض العملية بسبب عدم كفاية الرصيد    |
| ❌ البطاقة منتهية    | `tok_chargeDeclinedExpiredCard`       | `4000 0000 0000 0069`                 | البطاقة منتهية الصلاحية              |
| ❌ CVC غير صحيح      | `tok_chargeDeclinedIncorrectCvc`      | `4000 0000 0000 0127`                 | رمز الأمان CVC غير صحيح              |
| ❌ البطاقة مسروقة    | `tok_chargeDeclinedStolenCard`        | `4000 0000 0000 9979`                 | البطاقة مبلغ عنها إنها مسروقة        |
| ❌ البطاقة مفقودة    | `tok_chargeDeclinedLostCard`          | `4000 0000 0000 9987`                 | البطاقة مفقودة وتم رفضها             |
| ❌ رفض من البنك      | `tok_chargeDeclined`                  | `4000 0000 0000 0002`                 | البنك رفض العملية بدون تفاصيل        |
| ❌ خطأ في المعالجة   | `tok_chargeDeclinedProcessingError`   | `4000 0000 0000 0119`                 | حصل خطأ داخلي أثناء المعالجة         |
| ❌ احتيال مشتبه فيه  | `tok_chargeDeclinedFraudulent`        | `4100 0000 0000 0019`                 | Stripe اشتبهت في أن العملية احتيالية |

*/