# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability within Home-Decorator, please send an email to security@example.com. All security vulnerabilities will be promptly addressed.

## Security Practices

### Secret Management

- **GitHub**: Push Protection & secret-scanning on every branch to prevent leaked credentials
- **Local Development**: `dotnet user-secrets` store keeps tokens off disk
- **CI/CD**: GitHub Secrets referenced in workflows; never echoed
- **Cloud**: Azure Key Vault via `IConfiguration` with RBAC scoped
- **Database**: Only dynamic per-user tokens, column-level encryption

### Prohibited Practices

- Storing secrets in `appsettings.Production.json`
- Including credentials in scripts or Dockerfiles
- Committing any form of secret to source control

### Secret Rotation

- Rotation every 90 days minimum
- Key Vault & GitHub audit logs reviewed weekly
