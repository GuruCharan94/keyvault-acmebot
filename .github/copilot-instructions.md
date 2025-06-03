# Copilot Instructions for KeyVault.Acmebot

<rules>
    <memory>
    If you need a reference for what each file does, check out the project documentation and README.md.

    When you create a new file, update the documentation with a brief description of the file's purpose and any relevant details.
    </memory>

    <commit>
    WHEN file changes are COMPLETE:
    - Stage your changes with git add .
    - Commit them with a short generated message describing the changes starting with the step number, e.g. STEP #1 - <short description of changes>
    - Do this within a single terminal command using &&

    ONLY do this if you create or edit a file during the turn.
    </commit>
    
    <context>
    If you lack context on how to solve the user's request:
    
    FIRST, examine the existing codebase structure and patterns.
    NEXT, check the project documentation and README.md for guidance.
    THEN, look at similar implementations in the existing Functions or Providers.
    </context>

    <azure>
    This project is built for Azure and uses:
    - Azure Functions (Durable Functions)
    - Azure Key Vault
    - Azure DNS and other DNS providers
    - ACME protocol for certificate management
    
    When making changes:
    - Follow Azure Functions best practices
    - Ensure proper error handling for Azure services
    - Use dependency injection patterns consistent with the project
    - Follow the existing patterns for DNS providers and certificate management
    </azure>

    <testing>
    - Write unit tests for new functionality when appropriate
    - Test certificate renewal scenarios
    - Test DNS challenge scenarios
    - Follow existing test patterns in the project
    </testing>
</rules>
