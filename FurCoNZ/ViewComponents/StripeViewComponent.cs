﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using FurCoNZ.Services;
using FurCoNZ.ViewModels;
using FurCoNZ.Models;
using Microsoft.CodeAnalysis.Options;
using FurCoNZ.Configuration;
using Microsoft.Extensions.Options;
using FurCoNZ.Services.Payment;

namespace FurCoNZ.Components
{
    public class StripeViewComponent : ViewComponent
    {
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;
        private readonly StripeService _stripeService;
        private readonly IOptions<StripeSettings> _stripeOptions;

        public StripeViewComponent(IUserService userService, IOrderService orderService, StripeService stripeService, IOptions<StripeSettings> stripeOptions)
        {
            _userService = userService;
            _orderService = orderService;
            _stripeService = stripeService;
            _stripeOptions = stripeOptions;
        }

        public async Task<IViewComponentResult> InvokeAsync(int orderId)
        {
            var user = await _userService.GetCurrentUserAsync(HttpContext.RequestAborted);
            if (user == null)
                return View(); // Default view displays nothing to the end user.

            var order = await _orderService.GetUserOrderAsync(user, orderId, HttpContext.RequestAborted);
            if (order == null)
                return View(); // Default view displays nothing to the end user.

            if (order.AmountPaidCents >= order.TotalAmountCents)
                return View(); // Default view displays nothing to the end user.

            var lineItems = order.TicketsPurchased.GroupBy(t => t.TicketType, new TicketTypeComparer());

            Stripe.StripeConfiguration.SetApiKey(_stripeOptions.Value.SecretKey);

            var checkoutSessionOptions = new Stripe.Checkout.SessionCreateOptions
            {
                // TODO: Make PaymentMethodTypes configurable
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = user.Email,
                LineItems = lineItems.Select(l => new Stripe.Checkout.SessionLineItemOptions {
                    Name = l.Key.Name,
                    Amount = l.Key.PriceCents,
                    Description = l.Key.Description,
                    Currency = "NZD",
                    Quantity = l.Count(), // This need to be calculated
                }).ToList(),
                ClientReferenceId = $"order_{orderId}",
                PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
                {
                    // Pass in metadata that can be easily viewed from Stripe Dashboard
                    Metadata = new Dictionary<string, string>
                    {
                        {"client_id", user.Id.ToString() },
                        {"client_name", user.Name },
                        {"client_email", user.Email },
                        {"order_id", order.Id.ToString() },
                    },
                },
                SuccessUrl = Url.Action("Success", "Stripe", null, Request.Scheme),
                CancelUrl = Url.Action("Cancelled", "Stripe", null, Request.Scheme),
            };

            var subTotal = checkoutSessionOptions.LineItems.Sum(l => l.Amount.Value);
            // Calculate stripe fee to ensure received amount is what's requested.
            var stripeFee = ((subTotal + 30) * 1000 / 971) - subTotal;
            // calculate the actual fee that will be deducted by stripe during transfer.
            var actualFee = ((subTotal + stripeFee) * 29 / 1000) + 30;
            if (actualFee > stripeFee)
                stripeFee += 1; // correct for 1 cent error.
            
            if (_stripeOptions.Value.IncludeFee)
            {
                checkoutSessionOptions.LineItems.Add(new Stripe.Checkout.SessionLineItemOptions
                {
                    Name = "Stripe processing fees",
                    Amount = stripeFee,
                    Currency = "NZD",
                    Quantity = 1,
                });
            }

            var total = checkoutSessionOptions.LineItems.Sum(l => l.Amount.Value);

            var checkoutSessionService = new Stripe.Checkout.SessionService();
            var checkoutSession = await checkoutSessionService.CreateAsync(checkoutSessionOptions, cancellationToken: HttpContext.RequestAborted);

            await _stripeService.AddStripeSessionToOrderAsync(order.Id, checkoutSession.Id, HttpContext.RequestAborted);

            // Will display the strip payment form that submits to the StripeController.
            return View("Input", new StripeChargeViewModel
            {
                CheckoutSessionId = checkoutSession.Id,
                Order = new OrderViewModel(order),
                FeeCents = (int)stripeFee,
                TotalCents = (int)total,
            });
        }

        private class TicketTypeComparer : IEqualityComparer<TicketType>
        {
            public bool Equals(TicketType x, TicketType y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(TicketType obj)
            {
                return obj.Id;
            }
        }
    }
}
