# Feature Specification: ACME Renewal Information (ARI) Support

## 1. Overview
This feature enables KeyVault.Acmebot to consume ACME Renewal Information (ARI) from supported Certificate Authorities (CAs) such as Let's Encrypt. By leveraging ARI, the service can optimize certificate renewal timing and respond automatically to revocation events, improving reliability and reducing manual intervention.

## 2. User Journey
1. User configures KeyVault.Acmebot to use an ARI-supporting CA (e.g., Let's Encrypt).
2. The system automatically queries the ARI endpoint for each managed certificate during renewal checks (see details below).
3. If ARI signals a recommended renewal time or an upcoming revocation, the system schedules or triggers renewal accordingly.
4. The dashboard and webhooks surface ARI-driven renewal actions and events to the user.

## 3. Functional Requirements

### 3.1 ARI Query and Parsing
**Description**: The system must query the ARI endpoint for each managed certificate and parse the response for renewal and revocation information.
**Acceptance Criteria**:
- [ ] The system queries the ARI endpoint for each certificate during renewal checks, without caching in the initial implementation.
- [ ] The system correctly parses ARI responses, including renewal window and revocation signals.

### 3.2 Renewal Scheduling Based on ARI
**Description**: The system must adjust renewal scheduling logic to use ARI-provided renewal hints when available.
**Acceptance Criteria**:
- [ ] The system schedules renewals based on ARI-provided recommended times.
- [ ] The system falls back to static renewal intervals if ARI is unavailable or not supported by the CA.
- [ ] The renewal scheduling process is as follows:
    - The `RenewCertificates` function is triggered on a schedule (e.g., daily).
    - For each certificate, the function checks if it is within the renewal window (e.g., expiring within 30 days) by comparing the certificate’s `ExpiresOn` property from Key Vault to the current date.
    - If ARI is available, the function queries the ARI endpoint and uses the CA-provided renewal window or revocation signal to determine if renewal should be scheduled or triggered immediately.
    - If ARI is not available, the function uses the static renewal window logic.

### 3.3 Immediate Renewal on Revocation Signal
**Description**: The system must trigger immediate renewal if ARI signals an upcoming revocation or urgent renewal need.
**Acceptance Criteria**:
- [ ] The system triggers renewal immediately when ARI indicates revocation or urgent renewal.
- [ ] The system logs and surfaces these actions in the dashboard and via webhooks.
- [ ] ARI-driven lifecycle actions (renewal, revocation, etc.) are logged using Application Insights and surfaced via webhook notifications, consistent with existing certificate lifecycle actions.

### 3.4 Dashboard and Webhook Updates
**Description**: The dashboard and webhook payloads must include ARI-driven renewal and revocation events.
**Acceptance Criteria**:
- [ ] The dashboard displays ARI-driven renewal and revocation events.
- [ ] Webhook notifications include ARI-related actions.

### 3.5 Backwards Compatibility
**Description**: The system must maintain compatibility with CAs that do not support ARI.
**Acceptance Criteria**:
- [ ] The system continues to function as before for non-ARI CAs.
- [ ] No breaking changes are introduced for existing users.

## 4. Technical Considerations
- Update or extend the ACME protocol client to support ARI extension.
- Modify Durable Functions orchestrator logic to incorporate ARI data for scheduling and triggering renewals.
- Update models to store ARI metadata (e.g., next suggested renewal time, ARI status).
- Ensure secure and authenticated access to ARI endpoints.
- Implement robust error handling and retry logic for ARI queries.
- Ensure ARI-driven renewals do not bypass existing authentication/authorization checks.
- Ensure all ARI lifecycle actions (renewal, revocation, etc.) are logged using Application Insights, surfaced via webhooks, and visible in the dashboard, following the current process for certificate lifecycle auditing.
- **Initial implementation will not use any distributed or persistent cache for ARI responses.**

## 5. Integration Points
- **Functions:** `RenewCertificate`, `RenewCertificates`, and orchestrator logic for certificate lifecycle.
- **DNS Providers:** No changes required, but ensure compatibility with ARI-driven renewal timing.
- **Key Vault:** Standard certificate storage and update operations.
- **Webhooks:** Update payloads to include ARI-driven events.
- **Dashboard:** Update UI to display ARI-related information and actions.

## 6. Testing Strategy
- Unit tests for ARI query, parsing, and renewal logic.
- Integration tests with Let's Encrypt staging to simulate ARI-driven renewals and revocations.
- Regression tests to ensure legacy renewal logic works for non-ARI CAs.
- Manual testing of dashboard and webhook updates for ARI events.

---

**Please review this spec and let me know if you have feedback or additional requirements.**

**Additional considerations:**
- Consider how ARI data is cached or stored to avoid excessive queries. (Initial implementation will not use caching; see v2 enhancements below.)
- Ensure clear logging and auditability of ARI-driven actions.
- Monitor for changes in ARI specification (e.g., new fields, response formats, error codes, or CA operational practices) or CA support over time. Stay up-to-date with IETF ACME working group, CA changelogs, and ARI documentation to ensure ongoing compatibility.

**Future Enhancements (v2):**
- If ARI integration works well, implement ARI response caching using Azure Table Storage to reduce redundant API calls and improve efficiency. Store the ARI renewal window, revocation status, and timestamp for each certificate. On each renewal check, first look up the ARI data in Table Storage and only query the ARI endpoint if the cached data is stale or missing.
- Store all certificate lifecycle actions (add, renew, revoke, ARI-driven events) in Azure Table Storage for persistent, queryable audit logging. Each action should be recorded as a row with details such as certificate name, action type, timestamp, and relevant metadata. This will provide a durable audit trail beyond Application Insights and webhooks, supporting compliance and troubleshooting needs.
