FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files for layer caching
COPY ["src/Presentation/Identity.API/Identity.API.csproj", "src/Presentation/Identity.API/"]
COPY ["src/Infrastructure/Identity.Persistence/Identity.Persistence.csproj", "src/Infrastructure/Identity.Persistence/"]
COPY ["src/Core/Identity.Domain/Identity.Domain.csproj", "src/Core/Identity.Domain/"]
COPY ["src/Core/Identity.Application/Identity.Application.csproj", "src/Core/Identity.Application/"]
COPY ["src/Infrastructure/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Infrastructure/Identity.Infrastructure/"]

RUN dotnet restore "src/Presentation/Identity.API/Identity.API.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/Presentation/Identity.API"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configure ASP.NET Core inside container to listen on 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Identity.API.dll"]