using HomeDecorator.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HomeDecorator.Api.Controllers
{
    [ApiController]
    [Route("api/billing")]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly ICreditLedgerService _creditLedgerService;
        private readonly IFeatureFlagService _featureFlagService;

        public BillingController(
            IBillingService billingService,
            ICreditLedgerService creditLedgerService,
            IFeatureFlagService featureFlagService)
        {
            _billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
            _creditLedgerService = creditLedgerService ?? throw new ArgumentNullException(nameof(creditLedgerService));
            _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
        }

        [HttpGet("checkout/{packId}")]
        [Authorize]
        public async Task<IActionResult> GetCheckoutUrl(string packId)
        {
            try
            {
                // In a real app, get the userId from the authenticated user
                string userId = User.Identity?.Name ?? "test-user";

                // Get checkout URL from the billing service
                string checkoutUrl = await _billingService.GetCheckoutUrlAsync(userId, packId);

                return Ok(new { url = checkoutUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("portal")]
        [Authorize]
        public async Task<IActionResult> GetBillingPortal()
        {
            try
            {
                // In a real app, get the userId from the authenticated user
                string userId = User.Identity?.Name ?? "test-user";

                // Get portal URL from the billing service
                string portalUrl = await _billingService.GetBillingPortalUrlAsync(userId);

                return Ok(new { url = portalUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("credits")]
        [Authorize]
        public async Task<IActionResult> GetCredits()
        {
            try
            {
                // In a real app, get the userId from the authenticated user
                string userId = User.Identity?.Name ?? "test-user";

                // Get credit balance
                int balance = await _creditLedgerService.GetBalanceAsync(userId);

                return Ok(new { credits = balance });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("transactions")]
        [Authorize]
        public async Task<IActionResult> GetTransactions([FromQuery] int count = 20)
        {
            try
            {
                // In a real app, get the userId from the authenticated user
                string userId = User.Identity?.Name ?? "test-user";

                // Get transaction history
                var transactions = await _creditLedgerService.GetTransactionHistoryAsync(userId, count);

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
