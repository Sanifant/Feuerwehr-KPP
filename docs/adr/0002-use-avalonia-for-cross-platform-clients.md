# ADR-0002: Use Avalonia UI for Cross-Platform User Interfaces

* **Status:** Accepted
* **Date:** 2026-07-23
* **Decision Makers:** Stefan Petersen

## Context

Feuerwehr-KPP is intended to support operational and administrative workflows for fire services.

One planned use case is helping users locate nearby hydrants during an emergency response. The client must display current hydrant information and should later support offline access to previously downloaded hydrant locations.

The application is expected to run on:

* Android
* iOS
* Windows
* Linux

Support for embedded Linux devices may become relevant for future kiosk, vehicle-mounted, or IoT-related scenarios.

The project is primarily implemented with C# and .NET. Reusing the existing language, tooling, application services, models, and development experience is therefore an important consideration.

The following quality attributes influence the decision:

* high reuse of UI and presentation logic
* support for desktop and mobile platforms
* maintainability by a small development team
* access to platform-specific capabilities where required
* testable presentation logic
* support for offline-capable workflows
* the possibility of supporting Linux-based devices

## Decision

Avalonia UI will be used as the cross-platform UI framework for the Feuerwehr-KPP desktop and mobile clients.

Shared code should include, where practical:

* views
* view models
* validation
* navigation abstractions
* API clients
* domain-facing application logic
* styles and themes

Platform-specific functionality will be isolated behind interfaces and implemented in the relevant platform projects.

Examples include:

* location services
* application permissions
* secure storage
* file access
* notifications
* camera access
* platform-specific lifecycle handling

The decision to use Avalonia does not imply that every line of UI or platform integration must be shared.

## Alternatives Considered

### .NET MAUI

.NET MAUI supports Android, iOS, Windows, and macOS through Mac Catalyst.

#### Advantages

* supported by Microsoft
* native platform controls
* strong integration with the .NET ecosystem
* established mobile platform abstractions
* access to MAUI Essentials APIs

#### Disadvantages

* Linux is not an officially supported production target
* experimental Linux backends would introduce additional risk
* embedded Linux and direct framebuffer rendering are not standard deployment targets
* the project would require a separate solution for supported Linux clients

.NET MAUI was rejected because first-class Linux support is an explicit requirement.

### Kotlin Multiplatform and Compose Multiplatform

Kotlin Multiplatform can share application logic across Android, iOS, and desktop platforms. Compose Multiplatform can also provide shared user interfaces for Android, iOS, Windows, macOS, and Linux.

#### Advantages

* broad platform support
* strong Android integration
* shared UI through Compose Multiplatform
* production-ready support for mobile and desktop platforms
* access to native platform APIs

#### Disadvantages

* introduces Kotlin and Gradle into a primarily C# and .NET codebase
* requires additional development and maintenance expertise
* reduces reuse of existing .NET client libraries and models
* creates a second major technology ecosystem within the project
* increases onboarding and build-pipeline complexity

Kotlin Multiplatform was rejected because its additional language and tooling costs currently outweigh its platform-specific advantages.

### Separate Native Applications

Independent applications could be implemented with native technologies such as:

* Kotlin or Jetpack Compose for Android
* Swift or SwiftUI for iOS
* WPF or WinUI for Windows
* GTK or another Linux UI framework for Linux

#### Advantages

* best integration with each platform
* native user experience
* immediate access to platform APIs
* independent platform evolution

#### Disadvantages

* multiple codebases
* duplicated UI and presentation logic
* significantly higher implementation and testing effort
* increased risk of inconsistent behavior
* greater maintenance requirements
* unsuitable for the current team size

This alternative was rejected because the expected benefits do not justify maintaining four separate client implementations.

### Browser-Based Progressive Web Application

A responsive web application or progressive web application could support desktop and mobile devices through a browser.

#### Advantages

* one deployment artifact
* centralised updates
* broad device compatibility
* no application-store deployment required for basic usage

#### Disadvantages

* restricted platform integration
* browser-dependent offline behaviour
* limited background processing
* possible limitations for location, secure storage, notifications, and local hardware access
* less suitable for embedded Linux or direct framebuffer scenarios

This alternative may still be used for administrative functionality, but it is not the primary technology for operational cross-platform clients.

## Consequences

### Positive

* A substantial portion of UI and presentation logic can be shared across desktop and mobile platforms.
* The project remains within the existing C# and .NET technology ecosystem.
* Existing models, API clients, validation rules, and application services can be reused.
* Windows, Linux, Android, and iOS can be addressed with one primary UI framework.
* Linux desktop and future embedded Linux deployments remain possible.
* Shared XAML, styles, and view models can provide a consistent user experience.
* MVVM allows presentation logic to be tested independently of the rendered UI.
* Cross-platform changes can often be delivered through one coordinated implementation.

### Negative

* Platform-specific projects and implementations are still required.
* Android, iOS, desktop, and embedded Linux must each be tested independently.
* Mobile and desktop application lifecycle models differ and require separate startup configuration.
* Some device capabilities may require native interop or custom platform adapters.
* Third-party Avalonia controls may not support every target platform equally.
* Native platform conventions may require platform-specific views or styling.
* The development team must acquire and maintain Avalonia-specific knowledge.
* Avalonia has a smaller package and component ecosystem than some established native frameworks.
* A regression in shared code can affect several platforms simultaneously.
* Build, signing, packaging, and distribution remain platform-specific concerns.

### Operational Consequences

* Separate CI jobs are required for desktop, Android, and iOS builds.
* iOS compilation, signing, and publication require Apple-compatible build infrastructure.
* Releases must be tested using a defined platform matrix.
* Crash reporting and telemetry must identify the operating system and client version.
* Offline data must be stored using a cross-platform abstraction with platform-specific storage implementations where necessary.
* Database schema and offline-cache migrations must be tested on all supported client platforms.
* Permissions for location, storage, networking, and notifications must be handled separately for Android and iOS.

### Risks

* Individual Avalonia features may have different maturity levels across desktop and mobile platforms.
* Platform-specific defects may only become visible on physical devices.
* Required device APIs may not have suitable cross-platform abstractions.
* Embedded Linux devices may require hardware-specific rendering, input, or deployment configuration.
* Application-store policies may affect mobile deployment independently of the chosen UI framework.

### Mitigations

* Create an early proof of concept for Android, iOS, Windows, and Linux.
* Test location access, offline storage, navigation, and secure authentication before implementing the full feature set.
* Keep platform services behind explicit interfaces.
* Avoid placing device-specific logic in shared view models.
* Maintain automated tests for shared presentation and application logic.
* Maintain a documented platform-support matrix.
* Use physical-device tests for critical operational workflows.
* Reassess the framework if a mandatory platform capability cannot be implemented reliably.

## Decision Outcome

Avalonia UI is selected because it supports the required desktop and mobile operating systems while allowing the project to remain within the C# and .NET ecosystem.

Its support for Linux and embedded Linux is a significant advantage over .NET MAUI for the expected deployment scenarios.

This decision accepts that cross-platform development does not eliminate platform-specific engineering. Platform integrations, testing, packaging, signing, and selected user-interface adaptations will continue to be implemented separately.

## Review Triggers

This decision will be reviewed if:

* a required mobile capability cannot be implemented reliably
* mobile platform stability does not meet operational requirements
* platform-specific code becomes a significant portion of the client
* application-store deployment introduces unacceptable limitations
* the embedded Linux requirement is removed
* the team can no longer maintain the required Avalonia expertise
* a supported target platform is removed from Avalonia
* user testing shows that a shared UI cannot provide an acceptable native experience

## History

* 2026-07-23: Initial version
