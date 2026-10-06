FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY BookingSystem.API/BookingSystem.API.csproj BookingSystem.API/
COPY BookingSystem.Application/BookingSystem.Application.csproj BookingSystem.Application/
COPY BookingSystem.Domain/BookingSystem.Domain.csproj BookingSystem.Domain/
COPY BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj BookingSystem.Infrastructure/
RUN dotnet restore BookingSystem.API/BookingSystem.API.csproj

COPY . .
RUN dotnet publish BookingSystem.API/BookingSystem.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BookingSystem.API.dll"]
