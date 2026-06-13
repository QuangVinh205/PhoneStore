using PhoneStore.Dtos;

namespace PhoneStore.Service
{
    public interface IPayPalService
    {
        Task<string> CreatePaymentUrl(PaymentInformation model, HttpContext context);
        Task<PaymentResponse> PaymentExecute(IQueryCollection collections);
    }
}
