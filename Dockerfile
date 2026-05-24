# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY GK.GlobalKineticAssessment.sln .
COPY Directory.Build.props .
COPY global.json .

COPY src/GK.GlobalKineticAssessment.Domain/GK.GlobalKineticAssessment.Domain.csproj \
     src/GK.GlobalKineticAssessment.Domain/
COPY src/GK.GlobalKineticAssessment.Application/GK.GlobalKineticAssessment.Application.csproj \
     src/GK.GlobalKineticAssessment.Application/
COPY src/GK.GlobalKineticAssessment.Infrastructure/GK.GlobalKineticAssessment.Infrastructure.csproj \
     src/GK.GlobalKineticAssessment.Infrastructure/
COPY src/GK.GlobalKineticAssessment.API/GK.GlobalKineticAssessment.API.csproj \
     src/GK.GlobalKineticAssessment.API/
COPY tests/GK.GlobalKineticAssessment.Tests/GK.GlobalKineticAssessment.Tests.csproj \
     tests/GK.GlobalKineticAssessment.Tests/

RUN dotnet restore GK.GlobalKineticAssessment.sln

# Stage 2: Build & unit test
FROM restore AS build
WORKDIR /src
COPY . .
RUN dotnet build GK.GlobalKineticAssessment.sln -c Release --no-restore

RUN dotnet test tests/GK.GlobalKineticAssessment.Tests/GK.GlobalKineticAssessment.Tests.csproj \
    -c Release --no-build \
    --filter "Category=Unit" \
    --logger "console;verbosity=minimal"

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish src/GK.GlobalKineticAssessment.API/GK.GlobalKineticAssessment.API.csproj \
    -c Release -o /app/publish --no-build /p:UseAppHost=false

# Stage 4: Runtime (non-root)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p logs && chown appuser:appgroup logs
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "GK.GlobalKineticAssessment.API.dll"]
