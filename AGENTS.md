# AGENTS.md - AI Agent Development Guidelines

This document provides code rules and best practices for AI agents (like GitHub Copilot) working on this Blazor WebAssembly / .NET project.

---

## **1. Guiding Philosophy**

Our approach is guided by four principles:
*   **Pragmatic Architecture:** Vertical Slice first, adapting as needed for the complexity of the task.
*   **Automation-First:** Use the command line (dotnet, az, gh) for reproducible builds and deployments.
*   **Quality by Design:** Adhere to SOLID principles, apply Test-Driven Development, and practice proactive refactoring.
*   **Maintained Simplicity:** Keep the codebase clean, concise, and easy to understand.

---

## **2. Architecture**

*   **Primary Pattern:** Employ **Vertical Slice Architecture** as the primary pattern, organizing features into self-contained slices. Within each slice, apply **Clean Architecture principles** to separate business logic from infrastructure. For simple features where this is overkill, a more direct implementation within the slice is acceptable.
*   **Inspiration:** Draw inspiration from established patterns found in these repositories:
    *   `github.com/z3d/Starter-App-Dotnet-0`
    *   `github.com/ardalis/CleanArchitecture`
    *   `github.com/fullstackhero/dotnet-starter-kit`
*   **Code Conciseness:** Uphold the **Single Responsibility Principle.** If a C# or Razor file grows excessively complex (e.g., over 500 lines), it is a strong signal to refactor it into smaller, more focused components. The goal is clarity, not arbitrary line counts.
*   **Proactive Refactoring:** Regularly remove unused code and files. All deletions must be done through a Pull Request with a clear justification.

---

## **3. Project Setup & Structure**

*   **Project Organization:** The root directory will be organized as follows:
    *   `/src`: All source code (Blazor, .NET libraries). *(Currently at root level)*
    *   `/tests`: All test projects (Unit, Integration, Functional). *(Currently at root level)*
    *   `/docs`: All documentation (README.md, PRD.md, architecture diagrams).
    *   `/scripts`: Automation and utility scripts.
*   **Project Naming Convention:** All projects must be prefixed with `Po`, for example, `PoFunQuiz.Server`.
*   **Initial Scaffolding:**
    *   Generate projects using `dotnet new` commands.
    *   Configure `launchSettings.json` to start only the API project (PoFunQuiz.Server).
    *   Enable `dotnet watch` for hot reloading during development.

---

## **4. Backend & API Design**

*   **API Documentation:** Configure Swagger/OpenAPI from project inception for clear documentation and testing.
*   **Endpoint Testing:** Ensure API endpoints are testable with tools like `curl` or Postman.
*   **Global Exception Handling:** Implement global exception handling middleware that uses **Serilog** for detailed logging and returns a standardized **Problem Details (RFC 7807)** response to the client.

---

## **5. Frontend Development (Blazor)**

*   **Component Strategy:** Begin with built-in Blazor components. For advanced features, use our standard component library, **Radzen.Blazor**.
*   **Dependencies:** Reference external CSS and JS libraries via CDN to optimize performance.
*   **Branding:** Dynamically set the HTML `<title>` in the main layout to the application's name.
*   **React Frontend:** After I create a Blazor front end I may sometimes ask about creating a react front end. I want it to be hosted inside of the .net api project so I can easily switch between the two frontends.

---

## **6. Data Persistence**

*   **Default Storage:** Use **Azure Table Storage** for its scalability and cost-effectiveness, with the **Azurite emulator** for local development.
*   **Alternative Storage:** Obtain tech lead approval for features requiring relational data or complex transactions, where **Azure SQL** or **Cosmos DB** may be more suitable.
*   **Data Access:** Abstract data operations behind feature-specific interfaces (e.g., `IGameSessionService`). Avoid a generic repository pattern to create methods tailored to Table Storage's key structure, preventing inefficient queries.
*   **Naming Convention:** Azure Storage tables must follow the format `PoAppName[TableName]` (e.g., `PoFunQuizPlayers`).

---

## **7. Logging & Diagnostics**

*   **Framework:** Implement **Serilog** with console and rolling file sinks.
*   **Health Checks:** Implement a `/api/health` endpoint using ASP.NET Core's built-in Health Checks feature.
*   **Diagnostics UI:** Create a `/diag` page in the Blazor UI that calls the `/api/health` endpoint to display the connection status of critical dependencies.

---

## **8. Testing**

*   **Framework:** Use **xUnit** for all tests.
*   **Test-Driven Development Cycle:**
    1.  **Write the Test:** Define the feature's behavior with a failing unit, integration, or functional test.
    2.  **Implement the Logic:** Write the necessary backend code to make the test pass.
    3.  **Refactor:** Improve the code's design while ensuring all tests remain green.
    4.  **Build the UI:** With a tested and functional API, implement the Blazor UI components.
    5.  **Submit for Review:** A feature is complete when all tests pass and the Pull Request is approved.
*   **Test Responsibilities:**
    *   **Unit Tests:** Verify individual components or business logic in isolation.
    *   **Integration Tests:** Test a complete vertical slice with emulated infrastructure (e.g., Azurite).
    *   **Functional Tests:** Target the live API endpoint with HTTP requests to validate the entire pipeline.

---

## **9. Configuration and Secrets Management**

For private repositories where ease of use is prioritized, secrets will be managed directly within configuration files.
*   **Local Development:** Store development-specific keys and connection strings in `appsettings.Development.json`.
*   **Production:** Production secrets and keys will be stored in `appsettings.json` or Azure App Configuration.
*   **Security:** The GitHub repository **must** be private. The `.gitignore` file must be correctly configured to prevent accidental exposure of any local-only configuration files.

---

## **10. Dependency Management**

Favor reputable, well-maintained open-source libraries. All dependencies must be approved and documented to ensure security and license compliance. Regularly review and update dependencies to patch vulnerabilities and leverage new features.

---

## **11. Deployment to Azure**

*   **Tooling:** Use the **Azure Developer CLI (azd)** for all deployments.
*   **Infrastructure as Code:** Define all Azure resources in **Bicep templates**. The Azure Resource Group name must match the solution name (e.g., `PoFunQuiz`).
*   **CI/CD:** A **GitHub Action** will automatically build, test, and deploy the application to Azure on every push to the `main` branch.

---

## **12. Progressive Web App (PWA) Requirements**

*   **Manifest:** Ensure `manifest.webmanifest` is properly configured with app name, icons, theme colors, and display mode.
*   **Service Worker:** Implement a service worker (`service-worker.js`) for offline support and caching strategies.
*   **Installation:** The app must be installable on mobile devices as a PWA with proper icons and splash screens.

---

## **13. Responsive Design**

*   All UI components must be responsive and work well on both desktop and mobile devices.
*   Use Bootstrap's responsive grid system and Radzen.Blazor's built-in responsive components.
*   Test layouts in both portrait and landscape orientations on mobile devices.

---

## **14. Local Development Ports**

*   **HTTP:** `http://localhost:5000`
*   **HTTPS:** `https://localhost:5001`
*   **No CORS needed:** The Blazor client is hosted within the API project, so cross-origin requests are not required.
