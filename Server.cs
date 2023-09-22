using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Stripe;

namespace StripeExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
              .UseUrls("http://0.0.0.0:4242")
              .UseWebRoot("public")
              .UseStartup<Startup>()
              .Build()
              .Run();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // This is your test secret API key.
            StripeConfiguration.ApiKey = "sk_test_51Ns41zLrhjYTvBmBUM2caro78HV9XLwOZhdU21wIMrmCfe0EivRTTHcTm25JHSTQ06RXIOtZxmCsvN5HTVWOMm1600rGqVKN67";

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }

    [Route("create-payment-intent")]
    [ApiController]
    public class PaymentIntentApiController : Controller
    {
        [HttpPost]
        [Route("[action]")]
        public ActionResult Create(PaymentIntentCreateRequest request)
        {
            var paymentIntentService = new PaymentIntentService();
            var customerService = new CustomerService();
            var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
            {
                Customer = "cus_OgQ4sNG80kkVsO",
                Amount = CalculateOrderAmount(request.Items),
                Currency = "gbp",
                // In the latest version of the API, specifying the `automatic_payment_methods` parameter is optional because Stripe enables its functionality by default.
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },

                SetupFutureUsage = "off_session",
            });

            return Json(new { clientSecret = paymentIntent.ClientSecret });
        }

        [HttpPost]
        [Route("[action]")]
        public ActionResult PayAnother([FromQuery] string originalIntentId)
        {
            var paymentIntentService = new PaymentIntentService();

            var originalIntent = paymentIntentService.Get(originalIntentId);

            var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
            {
                Customer = originalIntent.CustomerId,
                Amount = 1400,
                Currency = "gbp",
                PaymentMethod = originalIntent.PaymentMethodId,
                Confirm = true,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                },
            });

            return Ok(paymentIntent.Id);
        }

        [HttpPost]
        [Route("[action]")]
        public IActionResult Refund([FromQuery] string intentId)
        {
            var refundService = new RefundService();
            var refund = refundService.Create(new RefundCreateOptions
            {
                PaymentIntent = intentId,
            });

            return Ok(refund);
        }

        [HttpPut]
        public ActionResult Cancel(string originalIntentId)
        {
            var paymentIntentService = new PaymentIntentService();

            var originalIntent = paymentIntentService.Get(originalIntentId);

            var paymentIntent = paymentIntentService.Cancel(originalIntentId);

            return Ok();
        }

        private int CalculateOrderAmount(Item[] items)
        {
            // Replace this constant with a calculation of the order's amount
            // Calculate the order total on the server to prevent
            // people from directly manipulating the amount on the client
            return 1400;
        }

        public class Item
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("Amount")]
            public string Amount { get; set; }
        }

        public class PaymentIntentCreateRequest
        {
            [JsonProperty("items")]
            public Item[] Items { get; set; }
        }
    }
}