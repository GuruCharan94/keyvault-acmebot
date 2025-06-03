---
applyTo: "**/*.cs"
---

### .NET Development Guidelines

RULES:

- Follow C# coding conventions and best practices
- Use async/await patterns consistently
- Implement proper error handling with try-catch blocks
- Use dependency injection following the existing patterns
- Follow the repository pattern used in the project
- Use proper logging with ILogger
- Implement proper disposal of resources (using statements)

### Azure Functions Specific

- Use Durable Functions patterns for orchestration
- Follow the existing patterns for Activity and Orchestrator functions
- Use proper binding attributes for Azure services
- Handle retries and error scenarios appropriately
- Use proper JSON serialization for function inputs/outputs

### Code Organization

- Keep functions focused and single-purpose
- Use the existing folder structure (Functions, Models, Options, Providers, Internal)
- Implement new DNS providers following the IDnsProvider interface
- Add new options classes for configuration as needed
- Follow the existing naming conventions
