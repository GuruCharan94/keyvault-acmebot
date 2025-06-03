---
applyTo: "**/*.cs"
---

### Creating Tests

RULES:

- Create unit tests (if appropriate) to ensure the user acceptance criteria is met
- Create integration tests for Azure Functions when appropriate
- Test certificate lifecycle scenarios (add, renew, revoke)
- Test DNS challenge scenarios for different providers
- Keep tests simple and focused
- Test boundary conditions and error scenarios
- Mock Azure services appropriately for unit tests

### Running Tests

RULES:

- Always run tests after making changes to the code
- Fix any broken tests before committing
- Use `dotnet test` to run all tests
- Ensure tests pass in both local and CI environments

### Test Structure

- Follow the existing test patterns in the project
- Use proper mocking for Azure services (Key Vault, DNS providers)
- Test both success and failure scenarios
- Include tests for edge cases and error handling
