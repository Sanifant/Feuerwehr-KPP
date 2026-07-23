# ADR-0001: Use a Monorepository

* **Status:** Accepted
* **Date:** 2026-07-23
* **Decision Makers:** Stefan Petersen

## Context

Feuerwehr-KPP consists of multiple applications and shared components:

* ASP.NET Core Web API
* Avalonia Desktop Client
* Avalonia Mobile Client
* Shared domain models
* Shared API contracts
* CI/CD pipelines
* Infrastructure configuration

These components evolve together and are typically released as a compatible set.
The project is currently maintained by a small team, where most contributors work across multiple parts of the system.

## Decision

We will use a **single Git repository (Monorepo)** for all applications and shared libraries.

The repository will contain:

* Backend
* Desktop application
* Mobile application
* Shared libraries
* Infrastructure
* Documentation
* CI/CD configuration

## Alternatives Considered

### Multiple repositories

Each application would have its own repository.

**Advantages**

* Smaller repositories
* Independent release cycles
* Clear ownership boundaries

**Disadvantages**

* Increased coordination effort
* More difficult dependency management
* Duplicate CI/CD configuration
* More complex version synchronization

### Hybrid approach

Separate repositories for applications with shared packages published through NuGet.
This approach introduces additional package management and release complexity that is currently unnecessary.

## Consequences

### Positive

* Single source of truth
* Simplified refactoring across applications
* Shared pull requests
* Consistent code style
* Shared CI/CD pipelines
* Easier onboarding

### Negative

* Larger repository size
* Longer clone time
* More complex pipeline configuration
* Requires clear project structure

## Decision Outcome

The benefits of a Monorepo outweigh the additional complexity for the current size of the project.
The decision will be revisited if the project grows significantly or independent release cycles become necessary.

## History

- initial Version