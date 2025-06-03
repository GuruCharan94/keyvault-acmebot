---
applyTo: "**/*.cs"
---

### Azure Development Guidelines

RULES:

- Follow Azure Functions best practices for performance and scalability
- Use managed identity for Azure service authentication when possible
- Implement proper retry policies for Azure service calls
- Use Azure SDK patterns consistently
- Handle throttling and rate limiting appropriately
- Implement proper logging for monitoring and troubleshooting

### Key Vault Integration

- Use Key Vault references for sensitive configuration
- Implement proper certificate lifecycle management
- Handle Key Vault access permissions correctly
- Use appropriate Key Vault pricing tiers for the workload
- Implement proper backup and recovery strategies

### DNS Provider Integration

- Follow the IDnsProvider interface pattern for new providers
- Implement proper credential management for DNS providers
- Handle DNS propagation delays appropriately
- Test with different DNS provider rate limits
- Implement proper error handling for DNS operations

### ACME Protocol

- Follow ACME v2 specification requirements
- Implement proper challenge validation
- Handle certificate lifecycle events correctly
- Implement proper renewal scheduling
- Handle ACME server limitations and errors

### Deployment

- Use ARM templates or Bicep for infrastructure deployment
- Follow Azure security best practices
- Implement proper monitoring and alerting
- Use Application Insights for telemetry
- Configure appropriate scaling settings
