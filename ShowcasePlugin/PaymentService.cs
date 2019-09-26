using System;
using System.Linq;
using Resto.Front.Api.V6;
using Resto.Front.Api.V6.Data.Orders;
using Resto.Front.Api.V6.Data.Payments;
using Resto.Front.Api.V6.Data.Security;
using Resto.Front.Api.V6.Editors;

namespace ShowcasePlugin
{
    public class PaymentService
    {
        private readonly ICredentials credentials;
        private readonly IOperationService operationService;
        private readonly IPaymentType cashPaymentType;

        public PaymentService(IOperationService operationService, ICredentials credentials)
        {
            this.operationService = operationService;
            this.credentials = credentials;
            cashPaymentType = operationService
                .GetPaymentTypes()
                .Single(x => x.Kind == PaymentTypeKind.Cash);
        }

        public void CloseForCash(Guid orderId)
        {
            var order = PluginContext.Operations.GetOrderById(orderId);
            ValidateOrder(order);

            operationService.PayOrderAndPayOutOnUser(credentials, order, cashPaymentType, order.ResultSum);
        }

        public void CloseOrder(Guid orderId, params Payment[] payments)
        {
            var order = PluginContext.Operations.GetOrderById(orderId);
            ValidateOrder(order);

            var editSession = operationService.CreateEditSession();
            DeleteActualPayments(order, editSession);
            AddPayments(order, editSession, payments);
            PluginContext.Operations.SubmitChanges(credentials, editSession);

            //reload order data
            order = PluginContext.Operations.GetOrderById(orderId);
            operationService.PayOrder(credentials, order);
        }

        private void AddPayments(IOrder order, IEditSession editSession, params Payment[] payments)
        {
            var unpaid = order.ResultSum;
            foreach (var payment in payments)
            {
                unpaid -= payment.Amount;
                editSession.AddExternalPaymentItem(
                    payment.Amount,
                    payment.IsProcessed,
                    payment.AdditionalData,
                    payment.PaymentType,
                    order);
            }

            if (unpaid > 0)
                editSession.AddPaymentItem(unpaid, null, cashPaymentType, order);
        }

        private void DeleteActualPayments(IOrder order, IEditSession editSession)
        {
            if (order.Payments.Count == 0)
                return;
            foreach (var actualPayment in order.Payments)
                if (actualPayment.IsExternal)
                    editSession.DeleteExternalPaymentItem(actualPayment, order);
                else
                    editSession.DeletePaymentItem(actualPayment, order);
        }

        private static void ValidateOrder(IOrder order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (order.Status != OrderStatus.Bill)
                throw new ArgumentException($"Unexpected order status {order.Status}. Expected order status is {OrderStatus.Bill}");
        }
    }
}