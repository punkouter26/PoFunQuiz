# PoFunQuiz - Testing Documentation

This document provides comprehensive information about the test suite for PoFunQuiz, including test types, execution instructions, and coverage details.

---

## **Test Structure**

The PoFunQuiz test suite is organized into three main categories:

### **1. Unit Tests**
Located in: `PoFunQuiz.Tests/`
- `OpenAIServiceTests.cs` - Tests for OpenAI integration logic
- `QuestionConsistencyTests.cs` - Validates question generation consistency
- `QuestionConsistencyVerificationTests.cs` - Verifies question structure

### **2. Integration Tests**
Located in: `PoFunQuiz.Tests/Integration/`
- `QuizControllerTests.cs` - Tests for Quiz API endpoints
- `HealthCheckTests.cs` - Tests for /api/health endpoint

### **3. End-to-End (E2E) Tests**
Located in: `PoFunQuiz.Tests/E2E/`
- `AppE2ETests.cs` - Playwright tests for UI functionality

---

## **Running Tests**

### **Run All Tests**
```powershell
dotnet test
```

### **Run Specific Test Categories**

**Integration Tests Only:**
```powershell
dotnet test --filter "FullyQualifiedName~Integration"
```

**Health Check Tests:**
```powershell
dotnet test --filter "FullyQualifiedName~HealthCheckTests"
```

**E2E Tests Only:**
```powershell
dotnet test --filter "FullyQualifiedName~E2E"
```

**Unit Tests Only:**
```powershell
dotnet test --filter "FullyQualifiedName~PoFunQuiz.Tests" --filter "FullyQualifiedName!~Integration" --filter "FullyQualifiedName!~E2E"
```

### **Run with Verbose Output**
```powershell
dotnet test --logger "console;verbosity=detailed"
```

---

## **Integration Test Coverage**

### **QuizControllerTests** (7 tests)
Tests the Quiz API endpoints with various scenarios:

✅ **Passing Tests:**
- `GenerateQuestions_WithNegativeCount_ReturnsBadRequest` - Validates error handling for negative count
- `GenerateQuestions_WithZeroCount_ReturnsBadRequest` - Validates error handling for zero count
- `GenerateQuestions_WithExcessiveCount_ReturnsBadRequest` - Validates error handling for count > 10
- `GenerateQuestionsInCategory_WithNegativeCount_ReturnsBadRequest` - Validates category endpoint error handling
- `GenerateQuestionsInCategory_WithInvalidCategory_ReturnsBadRequest` - Validates invalid category handling

⚠️ **Expected Failures (requires Azure OpenAI configuration):**
- `GenerateQuestions_WithValidCount_ReturnsQuestions` - Requires real OpenAI credentials
- `GenerateQuestionsInCategory_WithValidParameters_ReturnsQuestions` - Requires real OpenAI credentials

**Theory-Based Data-Driven Tests:**
- `GenerateQuestions_WithVariousCounts_ReturnsExpectedCount` - Tests multiple count values (1, 5, 10)

### **HealthCheckTests** (3 tests)
Tests the ASP.NET Core Health Checks endpoint:

✅ **All Passing:**
- `HealthCheck_Endpoint_ReturnsOk` - Validates /api/health returns 200 OK
- `HealthCheck_Endpoint_ReturnsJson` - Validates response is proper JSON
- `HealthCheck_Endpoint_IncludesAllHealthChecks` - Validates all expected health checks are present

**Expected Health Checks:**
- `table_storage` - Azure Table Storage connectivity
- `openai_service` - Azure OpenAI service connectivity
- `internet` - Internet connectivity (microsoft.com)

---

## **E2E Test Coverage**

### **AppE2ETests** (14 tests)
Comprehensive Playwright tests covering UI functionality:

**Desktop Tests (1920x1080):**
- `HomePage_LoadsSuccessfully_Desktop` - Validates homepage renders
- `Navigation_ClickDiagnostics_NavigatesToDiagPage` - Tests diagnostics navigation
- `DiagnosticsPage_DisplaysHealthChecks` - Validates health check display

**Mobile Tests (390x844, 375x667):**
- `HomePage_LoadsSuccessfully_Mobile` - Validates homepage on mobile
- `ResponsiveDesign_MobileNavigation` - Tests mobile menu functionality

**Diagnostics Page Tests:**
- `DiagnosticsPage_LoadsSuccessfully` - Page loads without errors
- `DiagnosticsPage_RefreshHealthChecks_UpdatesStatus` - Refresh button works
- `DiagnosticsPage_DisplaysThreeHealthChecks` - Shows all 3 health checks

**PWA Tests:**
- `PWA_ManifestIsPresent` - Validates manifest.webmanifest exists
- `ServiceWorker_IsRegistered` - Validates service worker registration

**Accessibility Tests:**
- `Accessibility_PageHasProperHeadings` - Validates semantic HTML structure

**Performance Tests:**
- `PerformanceTest_PageLoadsWithin5Seconds` - Validates page load time < 5s

**Game Flow Tests:**
- `GameFlow_StartNewGame_CreatesSession` - Tests game initialization

---

## **Prerequisites**

### **For Integration Tests**
- .NET 9.0 SDK
- Microsoft.AspNetCore.Mvc.Testing (v9.0.10)
- xUnit (v2.9.3)

### **For E2E Tests**
- Playwright browsers installed
- Run this command if Playwright tests fail:
```powershell
pwsh PoFunQuiz.Tests/bin/Debug/net9.0/playwright.ps1 install chromium
```

### **For OpenAI Integration Tests**
To make OpenAI-dependent tests pass, configure these in `appsettings.Development.json`:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o"
  }
}
```

---

## **Test Execution Summary**

**Current Status (without Azure OpenAI credentials):**
- ✅ Health Check Tests: **3/3 passing** (100%)
- ⚠️ Quiz Controller Tests: **5/7 passing** (71%) - 2 require OpenAI config
- 🔄 E2E Tests: Require manual execution with `dotnet test --filter E2E`

**With Azure OpenAI configured:**
- ✅ All Integration Tests: **12/12 passing** (100%)
- ✅ Health Check Tests: **3/3 passing** (100%)

---

## **Continuous Integration**

The test suite is designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run Tests
  run: dotnet test --logger trx --results-directory TestResults
  
- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: .NET Tests
    path: TestResults/*.trx
    reporter: dotnet-trx
```

---

## **Known Limitations**

1. **OpenAI Tests**: Require valid Azure OpenAI credentials. Without them, tests return 0 questions and fail assertions.
2. **E2E Tests**: Require the application to be running on `https://localhost:5001` or the test creates its own instance.
3. **Playwright Installation**: First-time E2E test execution requires Playwright browser installation.

---

## **Future Test Enhancements**

- [ ] Add tests for `BrowserLogsController` endpoints
- [ ] Add tests for `DiagnosticsController` additional endpoints
- [ ] Implement mock Azure OpenAI service for integration tests
- [ ] Add load testing scenarios
- [ ] Add more accessibility tests (WCAG compliance)
- [ ] Add visual regression testing with Playwright screenshots
- [ ] Implement test data builders for complex scenarios

---

## **Troubleshooting**

### **Issue: Tests fail with "No such host is known"**
**Solution:** This is expected behavior without Azure OpenAI credentials. Configure `appsettings.Development.json` with valid credentials.

### **Issue: E2E tests fail with "Browser not found"**
**Solution:** Install Playwright browsers:
```powershell
pwsh PoFunQuiz.Tests/bin/Debug/net9.0/playwright.ps1 install
```

### **Issue: "Program does not exist in namespace"**
**Solution:** Ensure `Properties/AssemblyInfo.cs` contains `[assembly: InternalsVisibleTo("PoFunQuiz.Tests")]`

---

## **Contact & Support**

For test-related issues, please refer to:
- **Architecture Guidelines:** `AGENTS.md`
- **Project Documentation:** `README.md`
- **Development Guidelines:** `.github/copilot-instructions.md`
