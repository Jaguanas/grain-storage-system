FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Build context is the repository root (simplerpc/), see Lab1/README.md
COPY Lab1/Storage/Storage.csproj Lab1/Storage/
COPY Shared/StorageContract/StorageContract.csproj Shared/StorageContract/
COPY Shared/StorageCore/StorageCore.csproj Shared/StorageCore/
RUN dotnet restore Lab1/Storage/Storage.csproj

COPY . .
RUN dotnet publish Lab1/Storage/Storage.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 5000

ENTRYPOINT ["dotnet", "Storage.dll"]
