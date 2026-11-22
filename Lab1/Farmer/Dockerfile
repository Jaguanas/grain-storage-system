FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Build context is the repository root (simplerpc/), see Lab1/README.md
COPY Lab1/Farmer/Farmer.csproj Lab1/Farmer/
COPY Shared/StorageContract/StorageContract.csproj Shared/StorageContract/
RUN dotnet restore Lab1/Farmer/Farmer.csproj

COPY . .
RUN dotnet publish Lab1/Farmer/Farmer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build app/publish .
EXPOSE 5000

ENTRYPOINT [ "dotnet", "Farmer.dll" ]
