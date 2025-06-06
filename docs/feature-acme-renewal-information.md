# Feature Proposal: ACME Renewal Information (ARI) Support

## 1. Feature Overview
- **Core value proposition:**
  - Enable KeyVault.Acmebot to consume ACME Renewal Information (ARI) from supported CAs (e.g., Let's Encrypt) to optimize certificate renewal timing and respond automatically to revocation events.
- **How it enhances certificate management capabilities:**
  - Improves resiliency and reliability by allowing the service to renew certificates at the most appropriate time, as signaled by the CA, and to react to revocation events without manual intervention.
- **Target use cases:**
  - Organizations using Let's Encrypt or other ARI-supporting CAs who want automated, CA-driven renewal and revocation response for their Azure-hosted certificates.

## 2. Functional Specifications
- 2.1. Query ARI endpoint for each managed certificate to determine recommended renewal time and revocation status.
- 2.2. Adjust renewal scheduling logic to use ARI-provided renewal hints instead of static intervals when available.
- 2.3. Trigger immediate renewal if ARI signals an upcoming revocation or urgent renewal need.
- 2.4. Fallback to current renewal logic if ARI is not supported by the CA or not available for a certificate.
- 2.5. Log and surface ARI-driven renewal actions in the dashboard and via webhooks.

## 3. Technical Specifications
- Update the ACME protocol client to support ARI extension (see IETF draft and Let's Encrypt docs).
- Extend the certificate renewal orchestrator to:
  - Query ARI for each certificate during renewal checks.
  - Parse and interpret ARI response (renewal window, revocation signals).
  - Adjust Durable Functions timers and triggers based on ARI data.
- Update models to store ARI metadata (e.g., next suggested renewal time, ARI status).
- Update dashboard and webhook payloads to include ARI-driven events.
- Ensure backwards compatibility for CAs that do not support ARI.

## 4. Implementation Scope
- Phase 1: Add ARI support to ACME client and renewal orchestrator (behind a feature flag if needed).
- Phase 2: Update dashboard and webhooks to surface ARI-driven events.
- Phase 3: Gather feedback and iterate; enable by default if stable.
- No breaking changes expected; fallback to current logic if ARI is unavailable.

## 5. Testing Strategy
- Unit tests for ARI parsing and renewal logic.
- Integration tests with Let's Encrypt staging to simulate ARI-driven renewals and revocations.
- Regression tests to ensure legacy renewal logic works for non-ARI CAs.
- Dashboard and webhook test cases for ARI events.

## 6. Security Considerations
- Validate and sanitize all ARI data received from CAs.
- Ensure ARI-driven renewals do not bypass existing authentication/authorization checks.
- Maintain secure access to Key Vault and DNS providers during ARI-triggered operations.
- Log ARI events for auditability and troubleshooting.

---

For a high-level understanding of the project, see [docs/overview.md](overview.md).
