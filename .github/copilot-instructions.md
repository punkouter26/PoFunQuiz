LLM coding rules:

GENERAL:
- Use SOLID principles and Gang of Four patterns when applicable, prioritizing maintainability, readability, and scalability, and make a note in code comments when these are used.
- Use the latest version of all frameworks and tools 
- Use realistic dummy data in place of real data if needed, ensuring that each step can be compiled and tested. Ensure that dummy data closely resembles the expected format of real data.
- Add debug statements in the code in places that could be helpful, printing information to the console to aid in debugging as needed.
- After each of the 10 high level steps (steps.md) confirm with user that the program compiles and runs as expected
- Use best practices for UI Design, including responsiveness, mobile ready, and an intuitive user experience (UX).

.NET:
- First step create the dotnet templates as needed using the dotnet command
- If using external APIs that require keys then create tests that verifies basic connections to these APIs and ask me for the keys and I will find them and give them to you to add
- Add in any nuget packages needed with the dotnet command 
- Put all projects created on the root level in their own folder / The .sln file should be in the same folder level as all of the project folder / don’t put a project inside another project

TESTING/DEBUGGING:
- After each of the 10 high level coding steps (steps.md) tell the user if it is ready to test and allow user to manually test before moving to the next step / program should build, run , pass tests, pass manual tests before moving to the next step. Get confirmation from user that the code is compiling and running as expected before moving to the next step (steps.md)
- create Xunit test project to test the functionality before creating the UI for each of the 10 high level steps
- Always log to a file in the logs folder that contains all the info from the previous run so the AI coding LLM can use that to help debug
- refer to steps.md in the root directory to stay aware of the 10 high level steps that need to be done and remind the user often which steps are complete/incomplete

AZURE:
- Get app working locally first before deploying to Azure cloud
- Create resources as needed in cloud using Azure CLI / PowerShell script 
- Create a resource group named after the app name and store all resources in there / If you need info about the details of a resource use the Azure CLI to query this info
- use azure cli to create and obtain keys as needed to avoid asking the user to manually set up resources and get keys
- Use Azure CLI, PowerShell Script,  bicep template as ways to deploy to Azure / include alot of error checking and logging
- Use Application Insights to log useful information that can then be used to help debug as needed

GIT:
- Add a gitignore file to the root of the project as the first step
- Use Github CI/CD to deploy app once it is finished and working locally

BLAZOR Rules: 
- Delete the Home.razor and create a new default home page for the app

GODOT Rules: (If working on GODOT project)
- A blank godot 4.x project will be provided as starting template
- Use Godot 4.x syntax only and C# as the language
- Change scene and nodes by editing the scene file (.tscn) / do not ask user to manually do actions in the editor unless there is no choice
- Use simple primitives during creation and testing / I will replace at the end of development with the real models
- Create all objects as nodes in the scene tree unless asked to create something programmatically
- Define the transforms and properties of every object in the scene in the node properties (not in code)
- DO ONE STEP AT A TIME AND THEN ASK ME TO MANUALLY TEST after each high level step

 

