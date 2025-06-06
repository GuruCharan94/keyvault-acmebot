---
mode: 'agent'
description: 'Create a proposal for new features'
---

I will ask you to enhance the KeyVault.Acmebot project with a new feature.

Let's join forces to make this idea into a simple, yet impactful proposal.

FIRST:
- Clarify any areas of my proposal that may need more details
- Suggest new requirements based on the certificate management functionality needed
- Consider edge cases that may not be included in my original proposal
- Consider DNS provider compatibility and certificate lifecycle management
- Organize requirements logically, and break them down into units that would make sense as user stories
- Raise any important technical considerations, like Azure service integrations, DNS challenges, certificate validation, and security implications

NEXT:
- Iterate with me until I tell you I am satisfied.

FINALLY:
- Once I tell you I am ready, create a plan in the docs/ folder as feature-[name].md with the following structure as a Markdown file:

```
# Feature Proposal: [Feature Name]

## 1. Feature Overview
- Core value proposition
- How it enhances certificate management capabilities
- Target use cases

## 2. Functional Specifications
- Break down the user's request into a series of functional specifications
- Extremely high level
- Number these 2.1, 2.2, 2.3, etc.
- Focus on certificate lifecycle operations
- Consider DNS provider integrations

## 3. Technical Specifications
- Azure Functions architecture changes needed
- Key Vault integration requirements
- DNS provider modifications required
- New models, options, or providers needed

## 4. Implementation Scope
- Provide a basic implementation scope that could be delivered incrementally
- Consider backwards compatibility
- Identify any breaking changes

## 5. Testing Strategy
- Unit testing requirements
- Integration testing scenarios
- Certificate management test cases
- DNS challenge test scenarios

## 6. Security Considerations
- Certificate security best practices
- Azure Key Vault access patterns
- DNS security implications
- Authentication and authorization requirements
```
