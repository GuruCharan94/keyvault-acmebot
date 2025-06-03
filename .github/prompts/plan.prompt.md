---
mode: 'edit'
description: 'Plan an implementation'
---

Your goal is to generate an implementation plan for a specification document provided to you.

RULES:
- Keep implementations simple, do not over architect
- Do not generate real code for your plan, pseudocode is OK
- For each step in your plan, include the objective of the step, the steps to achieve that objective, and any necessary pseudocode.
- Call out any necessary user intervention required for each step
- Consider security and Azure best practices part of each step and not a separate step
- Follow existing patterns in the KeyVault.Acmebot codebase

FIRST:

- Review the README.md file to understand the project structure and capabilities.
- Review the attached specification document to understand the requirements and objectives.
- Examine existing Functions, Providers, and Models to understand current patterns.

THEN:
- Create a detailed implementation plan that outlines the steps needed to achieve the objectives of the specification document.
- The plan should be structured, clear, and easy to follow.
- Structure your plan as follows, and output as Markdown code block

```markdown
# Implementation Plan for [Spec Name]

## Overview
[Brief description of what will be implemented]

## Steps

- [ ] Step 1: [Brief title]
  - **Task**: [Detailed explanation of what needs to be implemented]
  - **Files**: [Maximum of 20 files, ideally less]
    - `KeyVault.Acmebot/Functions/NewFunction.cs`: [Description of changes], [Pseudocode for implementation]
    - `KeyVault.Acmebot/Providers/NewProvider.cs`: [Description of changes], [Pseudocode for implementation]
  - **Dependencies**: [Dependencies for step]
  - **Azure Services**: [Any new Azure services or configurations needed]

[Additional steps...]

- [ ] Step N-2: Build and test locally
  - **Task**: Build the project and test with `func start`
  - **Files**: Verify all files compile successfully
  - **Dependencies**: Azure Functions Core Tools

- [ ] Step N-1: Write unit tests
  - **Task**: Create unit tests for new functionality
  - **Files**: Test files following existing patterns
  - **Dependencies**: Test framework setup

- [ ] Step N: Run all tests
  - **Task**: Execute `dotnet test` to ensure all tests pass
  - **Files**: All test files
  - **Dependencies**: None
```

NEXT:

- Iterate with me until I am satisfied with the plan

FINALLY: 

- Output your plan in the docs/ folder as plan-[feature-name].md
- DO NOT start implementation without my permission.
