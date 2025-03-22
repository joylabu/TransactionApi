# Use the official .NET 6 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TransactionApi.csproj", "./"]
RUN dotnet restore "TransactionApi.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build --configuration Release --no-restore -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish --configuration Release --no-restore -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TransactionApi.dll"]
