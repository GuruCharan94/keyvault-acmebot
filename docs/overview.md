# KeyVault.Acmebot Project Overview

## Purpose
KeyVault.Acmebot is an automated solution for issuing and managing SSL/TLS certificates using the ACME protocol, with certificates securely stored in Azure Key Vault. It is designed to support Azure services such as App Service, Container Apps, Application Gateway, Front Door, CDN, and more.

## Core Features
- Automated issuance and renewal of certificates via ACME v2-compliant CAs (Let's Encrypt, Buypass, ZeroSSL, etc.)
- Centralized certificate storage in Azure Key Vault
- Support for multiple DNS providers for DNS-01 challenges (Azure DNS, Cloudflare, Route53, GoDaddy, etc.)
- Dashboard for certificate management
- Webhook notifications for lifecycle events
- Integration with Application Insights for monitoring

## Architecture
- **Azure Functions (Durable Functions):** Orchestrate certificate lifecycle operations (issue, renew, revoke) using orchestrator and activity functions in the `Functions/` folder.
- **Dependency Injection:** Services, options, and providers are registered in `Startup.cs` using .NET DI patterns.
- **DNS Providers:** Pluggable provider model in `Providers/` for DNS-01 challenge automation. Each provider implements `IDnsProvider`.
- **Options Pattern:** Configuration for ACME, Key Vault, and DNS providers is managed via strongly-typed options classes in `Options/`.
- **Internal Utilities:** Helpers for ACME protocol, payload building, and Azure environment abstraction in `Internal/`.
- **Models:** Data structures for certificates, DNS zones, and challenge results in `Models/`.
- **Web UI:** Static dashboard and management pages in `wwwroot/`.

## Certificate Lifecycle
1. **Add Certificate:** User requests a new certificate. The orchestrator coordinates DNS challenge, certificate issuance, and storage in Key Vault.
2. **Renew Certificate:** Automated or manual renewal triggers the orchestrator to repeat the challenge and renewal process.
3. **Revoke Certificate:** Orchestrator handles revocation with the CA and updates Key Vault.

## DNS Challenge Automation
- The orchestrator selects the correct DNS provider based on configuration and domain.
- Providers automate the creation and cleanup of DNS-01 challenge records.

## Configuration
- All settings (ACME endpoint, Key Vault URL, DNS provider credentials, etc.) are managed via `AcmebotOptions` and related options classes.

## Extensibility
- New DNS providers can be added by implementing `IDnsProvider` and registering in `Startup.cs`.
- Additional ACME CAs can be supported via configuration.

## Security
- Uses Azure Managed Identity and `DefaultAzureCredential` for secure access to Key Vault and Azure resources.
- Follows best practices for certificate and secret management.

## Testing
- Unit and integration tests are recommended for all new features, especially for certificate lifecycle and DNS challenge scenarios.

---

**Reference this file for a high-level understanding of the project when designing or implementing new features.**
