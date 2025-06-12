General AI Assistant Rules:
Strictly adhere to the 10 steps in steps.md.
Mark completed steps: - [x] Step X: Description.
Pause and request user confirmation after each step.
Reference prd.md for product requirements (in the root directory, never modify).
Prioritize simplicity and future expandability in design.
Regularly check steps.md for progress tracking.
Focus on functional correctness before premature optimization.
Use SOLID principles and C# Design Patterns / Make comments in class files that explain the SOLID or GoF design pattern used when one is used
Project Setup:
Repository Initialization:
Create a .gitignore file for .NET projects
Set up simple CI/CD GitHub workflow files in .github/workflows to build and deploy the Azure app Service (The .net core web api) (same name as the solution).
Solution Structure:
Root directory: PoYourSolutionName/.
Solution file: PoYourSolutionName.sln in the root.
Tracking file: steps.md in the root.
Requirements file: prd.md in the root (contains solution name).
Debug log: Single log.txt file in the root (overwritten on each run).
GitHub workflows: .github/workflows/.
Each project's .csproj file will be directly in the root.
Project Creation & Configuration:
Create required projects using dotnet new in their respective root folders.
Use best practices for .csproj naming (e.g., PoYourSolutionName.Client.csproj).
Target .NET 9.x 
Create a Blazor WebAssembly csproj and a .NET core Web API csproj / Host the Blazor client app inside the server app (hosted)
Create .vscode directory with configured launch.json, tasks.json, for local F5 deployment.
Set the application name as the page title for all pages (Name when page is bookmarked)


Azure Resource Setup & Configuration:
Development and Deployment Phasing: Local development and functionality first; Azure deployment is a final step.
Use Azurite for local table storage emulation.
Transition to Azure Table Storage on deployment.
Azure Deployment Strategy:
Use azd up for initial deployment, which will create a resource group named after the application.
Utilize Azure Table Storage (if used) as the primary database with SAS connection strings.
Select the minimum Azure resource tier.
After initial azd up, use GitHub CI/CD for subsequent deployments.
Configure the shared Application Insights resource located in the PoShared resource group.
Store sensitive configuration:
Locally: appsettings.Development.json.
Azure: App Service configuration settings (environment variables) and appsettings.json.
Prefer Azure CLI over the portal/UI for configuration.
Use the existing PoShared resource group for shared Azure resources (App Service Plan, Azure OpenAI, etc.).
Use Azure CLI (az) and GitHub CLI (gh) to retrieve configuration information.
Use the existing Log Analytics resource in the PoShared resource group.
Create a YML file in the root folder for the App Service (Web API) and potentially Azure Table Storage (if used) / Use azd up with this YML file to create new resource group and resources.
Mandatory Diagnostics Page (Diag.razor - Client Project):
Automatically create a Diag.razor page accessible via /diag.
Communicate with the Server project to verify connections.
Display connection statuses in a clear grid (green/red).
Verify and display status of:
Data connections (Azure Table Storage/Azurite).
API health checks.
Internet connection.
Authentication services (if used per prd.md).
Any other critical dependencies in the code
Log all diagnostic results to Application Insights, console, Serilog, and log.txt.


Development Approach:
Architecture Selection: Decide between Vertical Slice Architecture (feature folders/CQRS) or Onion Architecture based on prd.md and best practices. If the app is simple and there these patterns are overkill then do something more simple
Implementation Guidelines:
Keep classes under 500 lines if possible.
Note significant design patterns in comments (e.g., // Using Observer Pattern).
Create realistic dummy data for development/testing.
Designate Home.razor as the primary landing page.
Create the 10 high-level steps in steps.md such that each step results in runnable code with a visually demonstrable UI.
Step Workflow & User Interaction 
Test Stubs (XUnit).
Implement Logic & UI.
Track Progress in steps.md.
Request Confirmation:
I've completed Step X: [Step Description].
The code compiles and all relevant tests pass.
Would you like me to:
1. Make adjustments to the current step
2. Proceed to Step Y: [Next Step Description]
Wait for user confirmation.
Logging & Diagnostics Strategy:
Comprehensive Logging: Console, Serilog, and log.txt.
 File:
Created/overwritten in the root on each run.
Contains the most recent server and client (if feasible) information for debugging.
The AI assistant must analyze log.txt after each run for errors/clues when there are runtime errors
Log Content Requirements: Timestamps, component names, operation context, detailed logging for key events, log key decisions/state changes (avoid repetition).
Application Insights Integration: Track telemetry, create custom events, set up availability tests (use the shared resource in PoShared).
UI Development (Blazor WebAssembly):
Implement responsive design.
Use Radzen Blazor UI library if enhanced controls are needed and the application complexity warrants it.
Error Handling & Reliability:
Global exception handler middleware (API).
try/catch at service boundaries.
Appropriate HTTP status codes.
DEtailed UI error messages to help debugging
Log exceptions with context.
Consider Circuit Breaker for external services.
Dependency Injection (DI): Follow standard practices (Transient, Scoped, Singleton), register in Program.cs.
Authentication & Security: Implement Google or Microsoft authentication via Azure Entra ID if required by prd.md. 
Testing Approach: Write XUnit tests for all new functionality (focus on business logic and core functionality), include descriptive debug statements, verify API connections with test data, and create dedicated connection tests for external APIs requiring keys / Calling the API should be one of the first steps before doing anything else
Data Storage & Management:
Timeline & Location: Development (Azurite in AzuriteData/, add to .gitignore), Production (Azure Table Storage).
Azure Table Storage Implementation: Primary data store, repository patterns, optimized keys, error handling, SAS connection strings (local in appsettings.Development.json, Azure in App Service env vars and appsettings.json).
Deployment Process:
Local functionality first.
Use azd for initial Azure resource deployment.
Configure environment-specific settings (only development/running locally and production/Azure)
Use GitHub Actions for CI/CD after initial azd to the application's resource group.
NuGet Package Management: Use dotnet add package for stable packages (no preview/beta), document package purpose.
UPDATE:
For deployment and CI CD do only what is needed to build and deploy the app specific resources that go into Azure
