using System;
using System.Linq;
using Resto.Front.Api.V6;
using Resto.Front.Api.V6.Data.Payments;
using Resto.Front.Api.V6.Exceptions;

namespace ShowcasePlugin
{
    internal class Showcases
    {
        private readonly IOperationService operationService;
        private readonly IPaymentType cardPaymentType;
        private readonly IPaymentType bonusPaymentType;
        private readonly PaymentService paymentService;
        private readonly ILog logger;

        public Showcases(IOperationService operationService, ILog logger, string pin, IPaymentType cardPayment, IPaymentType bonusPayment)
        {
            this.operationService = operationService;
            this.logger = logger;
            var credentials = operationService.AuthenticateByPin(pin);
            paymentService = new PaymentService(operationService, credentials);

            cardPaymentType = cardPayment;
            bonusPaymentType = bonusPayment;
        }

        private void IikoSample()
        {
            //Can not compile  IIKO from https://iiko.github.io/front.api.doc/v6/ru/Payments.html
            /*
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var credentials = PluginContext.Operations.AuthenticateByPin("777");
            var paymentType = operationService.GetPaymentTypesToPayOutOnUser().First(x => x.IsCash);
            PluginContext.Operations.PayOrderAndPayOutOnUser(credentials, order, paymentType, order.ResultSum);
            */
        }

        internal bool PayOutOnUser_With_Cash(Guid orderId)
        {
            try
            {
                //https://iiko.github.io/front.api.sdk/v6/html/M_Resto_Front_Api_IOperationService_PayOrderAndPayOutOnUser.htm
                logger.Info("Pay order for cash using PayOrderAndPayOutOnUser method");
                paymentService.CloseForCash(orderId);
                return true;
            }
            catch (PaymentActionFailedException e)
            {
                //Got exception with message "Нет подходящего типа внесения."
                logger.Error($"Can not close order {orderId} for cash", e);
                return false;
            }
        }

        internal bool PayOrder_With_Cash(Guid orderId)
        {
            try
            {
                //https://iiko.github.io/front.api.sdk/v6/html/M_Resto_Front_Api_IOperationService_PayOrder.htm
                logger.Info("Pay order for cash using PayOrder method");
                paymentService.CloseOrder(orderId);
                return true;
            }
            catch (PaymentActionFailedException e)
            {
                //Got exception with message "В заказе уже есть элементы оплаты."
                logger.Error($"Can not close order {orderId} for cash", e);
                return false;
            }
        }

        internal bool PayOrder_With_ExternalPayments_Without_AdditionalData(Guid orderId)
        {
            try
            {
                logger.Info("Close order with external payments when additionalData is NULL ");
                CloseForExternalPayment(orderId, null);
                return true;
            }
            catch (ArgumentNullException e)
            {
                //Got exception with message "Значение не может быть неопределенным. Имя параметра: additionalData"
                logger.Error($"Can not close order {orderId} for external payments", e);
                return false;
            }
        }

        internal bool PayOrder_With_ExternalPayments_And_Use_CardPaymentItemAdditionalData(Guid orderId)
        {
            try
            {
                var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
                logger.Info("Close order with external payments when additionalData is NOT NULL ");
                CloseForExternalPayment(orderId, additionalData);
                return true;
            }
            catch (InvalidCastException e)
            {
                //Got exception with message "Не удалось привести тип объекта "Resto.Front.Api.V6.Data.Payments.CardPaymentItemAdditionalData" к типу "Resto.Front.Api.V6.PaymentTypes.IikoNetPaymentItemAdditionalData"."
                //Type "IikoNetPaymentItemAdditionalData" is not available in this API...
                logger.Error($"Can not close order {orderId} for external payments", e);
                return false;

            }
        }

        private void CloseForExternalPayment(Guid orderId, object additionalData)
        {
            const bool isProcessed = true;
            var order = operationService.GetOrderById(orderId);
            var cardAmount = (int)order.FullSum / 2;
            var bonusAmount = order.FullSum - cardAmount;

            var cardPayment = new Payment(cardPaymentType, cardAmount, isProcessed, additionalData);
            var bonusPayment = new Payment(bonusPaymentType, bonusAmount, isProcessed, additionalData);

            paymentService.CloseOrder(orderId, cardPayment, bonusPayment);
        }
    }
}
