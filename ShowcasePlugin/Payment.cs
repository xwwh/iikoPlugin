using Resto.Front.Api.V6.Data.Payments;

namespace ShowcasePlugin
{
    public class Payment
    {
        public Payment(IPaymentType paymentType, decimal amount, bool isProcessed, object additionalData)
        {
            PaymentType = paymentType;
            Amount = amount;
            AdditionalData = additionalData;
            IsProcessed = isProcessed;
        }

        public IPaymentType PaymentType { get; }

        public decimal Amount { get; }

        public bool IsProcessed { get; }

        public object AdditionalData { get; }
    }
}