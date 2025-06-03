---
mode: 'agent'
description: 'Run the test suite'
---

FIRST:

- Run the unit tests with `dotnet test`
- Ensure all tests pass successfully
- If tests fail, review the error messages and fix the issues in the codebase
- If tests pass, proceed to the next step

THEN:

- Test Azure Functions locally with `func start`
- Verify all endpoints are accessible
- Test certificate operations if applicable
- Test DNS provider integrations if applicable
- If issues are found, review the error messages and fix the issues in the codebase
- If tests pass, proceed to the next step

FINALLY:

- Report back to the user with the results of the test suite
- Include any warnings or recommendations for deployment
- Suggest any additional testing that may be needed for the specific changes made
