using HomeDecorator.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Controllers
{
    [ApiController]
    [Route("api/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly IConfiguration _configuration;

        public StripeWebhookController(IBillingService billingService, IConfiguration configuration)
        {
            _billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();

            try
            {
                // Get the Stripe-Signature header value
                string signature = Request.Headers["Stripe-Signature"];

                if (string.IsNullOrEmpty(signature))
                {
                    return BadRequest("Missing Stripe-Signature header");
                }

                // Handle the webhook
                bool handled = await _billingService.HandleWebhookAsync(json, signature);

                if (handled)
                {
                    return Ok(new { received = true });
                }
                else
                {
                    return BadRequest("Failed to handle webhook");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling webhook: {ex.Message}");
                return BadRequest("Error processing webhook");
            }
        }
    }
}
