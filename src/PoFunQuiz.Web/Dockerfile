FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props and Directory.Packages.props for central package management
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Copy project files
COPY src/PoFunQuiz.Web/PoFunQuiz.Web.csproj src/PoFunQuiz.Web/
COPY src/PoFunQuiz.Core/PoFunQuiz.Core.csproj src/PoFunQuiz.Core/
COPY src/PoFunQuiz.Infrastructure/PoFunQuiz.Infrastructure.csproj src/PoFunQuiz.Infrastructure/
COPY src/PoFunQuiz.Shared/PoFunQuiz.Shared.csproj src/PoFunQuiz.Shared/
COPY src/PoFunQuiz.ServiceDefaults/PoFunQuiz.ServiceDefaults.csproj src/PoFunQuiz.ServiceDefaults/

# Restore dependencies
RUN dotnet restore src/PoFunQuiz.Web/PoFunQuiz.Web.csproj

# Copy source code
COPY src/ src/

# Build
WORKDIR /src/src/PoFunQuiz.Web
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PoFunQuiz.Web.dll"]
