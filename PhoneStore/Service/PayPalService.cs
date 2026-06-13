using PayPal.Core;
using PayPal.v1.Payments;
using PhoneStore.Dtos;
using System.Globalization;
using System.Net;

namespace PhoneStore.Service
{
    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _configuration;
        private const double ExchangeRate = 22_863.0;

        public PayPalService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static double ConvertVndToDollar(decimal vnd)
        {
            return Math.Round((double)vnd / ExchangeRate, 2);
        }

        private PayPalHttpClient CreateClient()
        {
            string? clientId = _configuration["Paypal:ClientId"];
            string? secretKey = _configuration["Paypal:SecretKey"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException("Missing Paypal:ClientId or Paypal:SecretKey.");
            }

            var envSandbox = new SandboxEnvironment(clientId, secretKey);
            return new PayPalHttpClient(envSandbox);
        }

        private static string FormatUsd(decimal vnd)
        {
            return ConvertVndToDollar(vnd).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private string BuildCallbackUrl(HttpContext context)
        {
            if (context.Request.Host.HasValue)
            {
                return $"{context.Request.Scheme}://{context.Request.Host}/fe/Home/PaymentCallback";
            }

            string? configuredReturnUrl = _configuration["PaymentCallBack:ReturnUrl"];
            if (!string.IsNullOrWhiteSpace(configuredReturnUrl))
            {
                return configuredReturnUrl;
            }

            throw new InvalidOperationException("Payment callback URL is not configured.");
        }

        public async Task<string> CreatePaymentUrl(PaymentInformation model, HttpContext context)
        {
            if (!model.Amount.HasValue || model.Amount <= 0)
            {
                throw new InvalidOperationException("Payment amount must be greater than 0.");
            }

            var client = CreateClient();
            var paypalOrderId = DateTime.Now.Ticks;
            var urlCallBack = BuildCallbackUrl(context);
            string formattedAmount = FormatUsd(model.Amount.Value);
            string orderDescription = string.IsNullOrWhiteSpace(model.Description)
                ? $"Order {paypalOrderId}"
                : model.Description.Trim();

            var payment = new Payment()
            {
                Intent = "sale",
                Transactions = new List<Transaction>()
                {
                    new Transaction()
                    {
                        Amount = new Amount()
                        {
                            Total = formattedAmount,
                            Currency = "USD",
                            Details = new AmountDetails
                            {
                                Tax = "0",
                                Shipping = "0",
                                Subtotal = formattedAmount,
                            }
                        },
                        ItemList = new ItemList()
                        {
                            Items = new List<Item>()
                            {
                                new Item()
                                {
                                    Name = "Order",
                                    Currency = "USD",
                                    Price = formattedAmount,
                                    Quantity = "1",
                                    Sku = paypalOrderId.ToString
                                        (CultureInfo.InvariantCulture),
                                    Tax = "0",
                                    Url = $"{context.Request.Scheme}://{context.Request.Host}/ViewCart"
                                }
                            }
                        },
                        Description = $"Invoice #{orderDescription}",
                        InvoiceNumber = paypalOrderId.ToString(CultureInfo.InvariantCulture)
                    }
                },
                RedirectUrls = new RedirectUrls()
                {
                    ReturnUrl = $"{urlCallBack}?payment_method=PayPal&success=1&order_id={paypalOrderId}",
                    CancelUrl = $"{urlCallBack}?payment_method=PayPal&success=0&order_id={paypalOrderId}"
                },
                Payer = new Payer()
                {
                    PaymentMethod = "paypal"
                }
            };

            var request = new PaymentCreateRequest();
            request.RequestBody(payment);

            var response = await client.Execute(request);
            var statusCode = response.StatusCode;

            if (statusCode is not (HttpStatusCode.Accepted or HttpStatusCode.OK or HttpStatusCode.Created))
            {
                return "";
            }

            var result = response.Result<Payment>();
            if (result.Links == null)
            {
                return "";
            }

            using var links = result.Links.GetEnumerator();

            while (links.MoveNext())
            {
                var link = links.Current;
                if (link == null) continue;
                if (!string.Equals(link.Rel?.Trim(), "approval_url", StringComparison.OrdinalIgnoreCase)) continue;
                return link.Href;
            }

            return "";
        }

        public async Task<PaymentResponse> PaymentExecute(IQueryCollection collections)
        {
            var response = new PaymentResponse();

            foreach (var (key, value) in collections)
            {
                string normalizedKey = key.ToLowerInvariant();

                if (normalizedKey == "order_description")
                {
                    response.OrderDescription = value;
                }

                if (normalizedKey == "transaction_id")
                {
                    response.TransactionId = value;
                }

                if (normalizedKey == "order_id")
                {
                    response.OrderId = value;
                }

                if (normalizedKey == "payment_method")
                {
                    response.PaymentMethod = value;
                }

                if (normalizedKey == "success")
                {
                    response.Success = int.TryParse(
                        value.ToString(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int successFlag) && successFlag > 0;
                }

                if (normalizedKey == "paymentid")
                {
                    response.PaymentId = value;
                }

                if (normalizedKey == "payerid")
                {
                    response.PayerId = value;
                }
            }

            if (!response.Success)
            {
                response.Status = "cancelled";
                return response;
            }

            if (string.IsNullOrWhiteSpace(response.PaymentId) || string.IsNullOrWhiteSpace(response.PayerId))
            {
                response.Success = false;
                response.Status = "invalid_callback";
                response.ErrorMessage = "PayPal callback is missing paymentId or PayerID.";
                return response;
            }

            try
            {
                var request = new PaymentExecuteRequest(response.PaymentId);
                request.RequestBody(new PaymentExecution
                {
                    PayerId = response.PayerId
                });

                var paypalResponse = await CreateClient().Execute(request);
                var payment = paypalResponse.Result<Payment>();

                response.TransactionId = payment.Id;
                response.Status = payment.State;
                response.Success = string.Equals(payment.State, "approved", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Status = "execute_failed";
                response.ErrorMessage = ex.Message;
            }

            return response;
        }
    }
}
