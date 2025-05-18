# Home-Decorator - AI-Driven Home Design Visualization SaaS

Mobile application that generates improved home decoration designs from uploaded images.

## Overview

Home-Decorator is a SaaS platform that allows users to snap a photo of an interior or exterior space and receive an AI-enhanced image showing an upgraded design. This mobile-first experience is powered by a .NET backend, SQLite database, and the DALL·E API.

## Features

- **AI-powered design visualization**: Upload a photo and get redesign suggestions
- **Mobile MVP Test-Harness**: Built with .NET MAUI (iOS + Android)
- **Stripe billing** for free-tier credits, one-time packs and subscriptions
- **AI-matched product recommendations** with price, vendor and affiliate URLs
- **Admin reconciliation tools** for Stripe events and internal credit ledger

## Architecture

- **Backend**: .NET Minimal API
- **Mobile**: .NET MAUI (iOS & Android)
- **Web**: Single Page Application
- **Database**: SQLite (with future migration path to PostgreSQL)
- **AI Integration**: DALL·E API
- **Cloud Storage**: S3-compatible storage
- **Payment Processing**: Stripe

## Getting Started

### Prerequisites

- .NET 8.0 SDK or higher
- Visual Studio 2022 or JetBrains Rider
- MAUI workload installed

### Development Setup

1. Clone the repository
   ```
   git clone https://github.com/yourusername/home-decorator.git
   cd home-decorator
   ```

2. Set up user secrets (no secrets in source control)
   ```
   dotnet user-secrets init --project src/HomeDecorator.Api
   dotnet user-secrets set "Stripe:ApiKey" "your-api-key" --project src/HomeDecorator.Api
   dotnet user-secrets set "DallE:ApiKey" "your-dalle-api-key" --project src/HomeDecorator.Api
   ```

3. Restore and build
   ```
   dotnet restore
   dotnet build
   ```

### Running the Application

- **API**: `dotnet run --project src/HomeDecorator.Api`
- **MAUI App**: Open in Visual Studio and run on your preferred emulator or device

## Security Practices

- **No secrets in source control**: Using GitHub Push Protection & secret scanning
- **Local Development**: `dotnet user-secrets` store
- **CI/CD**: GitHub Secrets referenced in workflows
- **Cloud**: Azure Key Vault via `IConfiguration` with RBAC
- **Database**: Column-level encryption for sensitive data

## Implementation Timeline

| Week | Track | Major Deliverables |
|------|-------|--------------------|
| **0** | 🏗 Foundation | Repo + CI/CD + GitHub secret policies. |
| **1** | 📱 Mobile Test-Harness | .NET MAUI shell, fake-data toggle, DI plumbing. |
| **2** | 💳 Billing Core | Stripe sandbox flow, credit ledger, webhooks. |
| **3** | 🖼️ Image Generation | DALL·E integration; thumbnail feed. |
| **4** | 🛍️ Product Matching | Object detection & retail API, "Shop this look". |
| **5** | 🧪 Hardening & QA | Regression tests, telemetry, secret-rotation playbook. |
