# ==========================================
# STAGE 1: Build & Publish (.NET 9 SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for optimal Docker layer caching
COPY ["src/Presentation/Identity.API/Identity.API.csproj", "src/Presentation/Identity.API/"]
COPY ["src/Infrastructure/Identity.Persistence/Identity.Persistence.csproj", "src/Infrastructure/Identity.Persistence/"]
COPY ["src/Infrastructure/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Infrastructure/Identity.Infrastructure/"]
COPY ["src/Core/Identity.Domain/Identity.Domain.csproj", "src/Core/Identity.Domain/"]
COPY ["src/Core/Identity.Application/Identity.Application.csproj", "src/Core/Identity.Application/"]
COPY ["src/Presentation/Identity.Contracts/Identity.Contracts.csproj", "src/Presentation/Identity.Contracts/"]

# Restore NuGet dependencies
RUN dotnet restore "src/Presentation/Identity.API/Identity.API.csproj"

# Copy full source tree and publish
COPY . .
WORKDIR "/src/src/Presentation/Identity.API"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# STAGE 2: Runtime (.NET 9 ASP.NET ASP.NET)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy compiled artifacts from Stage 1
COPY --from=build /app/publish .

# Expose internal container port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Identity.API.dll"]