---
mode: 'edit'
description: 'Plan a feature'
---

Your goal is to generate a functional spec for implementing a feature based on the provided idea.

Before generating the spec plan, be sure to review the README.md file to understand the project overview and existing capabilities.

RULES:
- Start by defining the user journey steps as simple as possible
- Number functional requirements sequentially
- Include acceptance criteria for each functional requirement
- Use clear, concise language
- Aim to keep user journey as few steps as possible to accomplish tasks
- Keep the UI simple and easy to digest
- Consider certificate lifecycle management (add, renew, revoke)
- Consider DNS provider integrations and challenges
- Consider Azure Key Vault integration patterns
- Follow existing Azure Functions patterns

STRUCTURE:

```markdown
# Feature Specification: [Feature Name]

## 1. Overview
[Brief description of the feature]

## 2. User Journey
1. [Step 1 of user interaction]
2. [Step 2 of user interaction]
3. [Step 3 of user interaction]

## 3. Functional Requirements

### 3.1 [Requirement Title]
**Description**: [What the system should do]
**Acceptance Criteria**:
- [ ] [Specific testable criteria]
- [ ] [Another specific testable criteria]

### 3.2 [Requirement Title]
**Description**: [What the system should do]
**Acceptance Criteria**:
- [ ] [Specific testable criteria]
- [ ] [Another specific testable criteria]

## 4. Technical Considerations
- Azure Functions architecture impact
- Key Vault integration requirements
- DNS provider considerations
- Security and authentication requirements
- Error handling and retry logic

## 5. Integration Points
- Existing Functions that may be affected
- DNS Providers that need updates
- Key Vault operations required
- Webhook notifications needed

## 6. Testing Strategy
- Unit test requirements
- Integration test scenarios
- Manual testing steps
```

NEXT:

- Ask me for feedback to make sure I'm happy
- Give me additional things to consider I may not be thinking about

FINALLY:

When satisfied:

- Output your plan in the docs/ folder as spec-[feature-name].md
- DO NOT start writing any code or implementation plans.
