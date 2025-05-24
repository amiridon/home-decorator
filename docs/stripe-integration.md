# Stripe Integration Guide

## Overview

The Home Decorator application uses Stripe for payment processing and credit purchases. This guide explains how to set up and use the Stripe integration in development and production environments.

## Prerequisites

- Stripe account with API keys
- .NET user secrets or Azure Key Vault for secure key storage

## Configuration

### Development Environment

1. **Set up User Secrets**:
   ```powershell
   dotnet user-secrets init --project src/HomeDecorator.Api
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_test_key" --project src/HomeDecorator.Api
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_your_test_key" --project src/HomeDecorator.Api
   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_your_webhook_secret" --project src/HomeDecorator.Api
   ```

2. **Toggle Fake Data Mode**:
   For development and testing, you can enable fake data mode in `appsettings.json`:
   ```json
   "FeatureFlags": {
     "IsFakeDataMode": true
   }
   ```

3. **Run Stripe CLI for Local Webhook Testing**:
   ```powershell
   stripe listen --forward-to http://localhost:5002/api/stripe/webhook
   ```

### Production Environment

In production, the application is configured to use Azure Key Vault to securely store Stripe API keys:

1. Store your Stripe keys in Azure Key Vault:
   - Stripe:SecretKey
   - Stripe:PublishableKey
   - Stripe:WebhookSecret

2. Ensure the `KeyVault` configuration section has the correct vault name:
   ```json
   "KeyVault": {
     "Enabled": true,
     "Name": "your-keyvault-name"
   }
   ```

3. Set up proper webhook endpoints in your Stripe dashboard pointing to your production URL.

## Credit Packs

The application offers the following credit packs:

1. **Starter Pack**: 50 credits for $4.99
2. **Standard Pack**: 200 credits for $14.99
3. **Premium Pack**: 500 credits for $29.99
4. **Professional Pack**: 1200 credits for $59.99

## Implementation Details

- `StripeService` class handles Stripe API calls and webhook processing
- `SqliteCreditLedgerService` manages the credit ledger in the database
- Webhook handler processes Stripe events to update the credit ledger
- Mobile app connects to the API endpoints to initiate payments

## Testing

To test the Stripe integration:

1. Set `IsFakeDataMode` to `false` in the API settings
2. Use Stripe test cards for payment testing (e.g., 4242 4242 4242 4242)
3. Check the credit ledger for successful transactions
4. Verify webhook handling for payment confirmations
