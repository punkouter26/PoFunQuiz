@echo off
echo Starting PoFunQuiz application...
echo.

rem Build the solution
dotnet build

if %ERRORLEVEL% neq 0 (
    echo Build failed with exit code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo Build successful!
echo Starting the application...
echo.
echo The application will be available at:
echo   https://localhost:7191
echo   http://localhost:5164
echo.

rem Run the application with the https profile
dotnet run --project PoFunQuiz.Web/PoFunQuiz.Web.csproj --launch-profile https --no-build
