---
applyTo: "**/*.cs"
---

### Building and Running

RULES:

- Use `dotnet build` to build the project on every iteration
- Use `dotnet test` to run tests on every iteration
- For Azure Functions, use `func start` for local development
- Only run the app locally when explicitly asked to
- Do not run the app until the plan is completed (unless asked by user)

### Local Development

To run this project locally:

1. Ensure you have the Azure Functions Core Tools installed
2. Set up local.settings.json with required configuration
3. Use `func start` to start the Azure Functions runtime locally

### Configuration

This project requires various Azure service configurations:
- Azure Key Vault access
- DNS provider configurations (Azure DNS, Cloudflare, etc.)
- ACME server settings

Refer to the README.md for detailed configuration instructions.
