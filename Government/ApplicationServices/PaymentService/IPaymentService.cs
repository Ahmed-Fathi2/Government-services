using Government.Contracts.Payment;

namespace Government.ApplicationServices.PaymentService
{
    public interface IPaymentService
    { 
        Task<Result<PaymentResponse>> MakeTransaction(string paymentMethodId, int requestId, decimal ServcieCost, string userId, string userName, string serviceName, CancellationToken cancellationToken = default!);
    }
}
