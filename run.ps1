# Run the application with explicit URL display
Write-Host "Starting PoFunQuiz application..." -ForegroundColor Cyan

# Build the solution
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host "Starting the application..." -ForegroundColor Cyan
Write-Host ""
Write-Host "The application will be available at:" -ForegroundColor Yellow
Write-Host "  https://localhost:7191" -ForegroundColor Green
Write-Host "  http://localhost:5164" -ForegroundColor Green
Write-Host ""

# Run the application with the https profile
dotnet run --project PoFunQuiz.Web/PoFunQuiz.Web.csproj --launch-profile https --no-build
