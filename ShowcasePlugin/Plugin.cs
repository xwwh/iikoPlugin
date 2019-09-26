using System;
using System.Collections.Generic;
using System.Linq;
using Resto.Front.Api.V6;
using Resto.Front.Api.V6.Attributes;
using Resto.Front.Api.V6.Data.Orders;
using Resto.Front.Api.V6.Data.Payments;
using Resto.Front.Api.V6.Exceptions;
using ShowcasePlugin.Properties;

namespace ShowcasePlugin
{
    [PluginLicenseModuleId(YOUR_LICENSE_CODE)]
    public class Plugin
    {
        const int YOUR_LICENSE_CODE = 00000018;

        public Plugin()
        {
            var logger = PluginContext.Log;

            //Currently, finding payments types by name is most simple way of getting plazius payment types (https://plazius.ru) 
            var paymentTypes = PluginContext.Operations.GetPaymentTypes();
            var cardPayment = paymentTypes.Single(x => x.Name == Settings.Default.CardPayment);
            var bonusPayment = paymentTypes.Single(x => x.Name == Settings.Default.BonusPayment);

            var showcases = new Showcases(
                PluginContext.Operations,
                logger,
                Settings.Default.Pin,
                cardPayment,
                bonusPayment
                );

            //take first order and try to close it
            var order = PluginContext.Operations
                .GetOrders()
                .First(x => x.Status == OrderStatus.Bill && x.ResultSum > 0);

            var cases = new Func<Guid, bool>[]
            {
                showcases.PayOutOnUser_With_Cash,
                showcases.PayOrder_With_Cash,
                showcases.PayOrder_With_ExternalPayments_Without_AdditionalData,
                showcases.PayOrder_With_ExternalPayments_And_Use_CardPaymentItemAdditionalData,
                o => {
                    logger.Error($"Failed to close order {o}");
                    return false;
                }
            };

            foreach (var func in cases)
            {
                if (!func(order.Id))
                    continue;
                logger.Info($"Order {order.Id} closed!");
                return;
            }
        }
    }
}
