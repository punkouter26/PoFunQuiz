FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props and Directory.Packages.props for central package management
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

# Copy project files
COPY src/PoFunQuiz.Web/PoFunQuiz.Web.csproj src/PoFunQuiz.Web/

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
