# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Se copian todos los .csproj primero para que el restore quede cacheado en su propia capa
COPY ["GestorTurnos.API/GestorTurnos.API.csproj", "GestorTurnos.API/"]
COPY ["GestorTurnos.Application/GestorTurnos.Application.csproj", "GestorTurnos.Application/"]
COPY ["GestorTurnos.CrossCutting/GestorTurnos.CrossCutting.csproj", "GestorTurnos.CrossCutting/"]
COPY ["GestorTurnos.Domain/GestorTurnos.Domain.csproj", "GestorTurnos.Domain/"]
COPY ["GestorTurnos.Infrastructure/GestorTurnos.Infrastructure.csproj", "GestorTurnos.Infrastructure/"]
RUN dotnet restore "./GestorTurnos.API/GestorTurnos.API.csproj"
COPY . .
WORKDIR "/src/GestorTurnos.API"
RUN dotnet build "./GestorTurnos.API.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GestorTurnos.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GestorTurnos.API.dll"]
