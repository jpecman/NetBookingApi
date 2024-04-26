FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BookingApi/BookingApi.csproj", "BookingApi/"]
COPY ["BookingApi.Domain/BookingApi.Domain.csproj", "BookingApi.Domain/"]
COPY ["BookingApi.Common/BookingApi.Common.csproj", "BookingApi.Common/"]
COPY ["BookingApi.Infrastructure/BookingApi.Infrastructure.csproj", "BookingApi.Infrastructure/"]
RUN dotnet restore "BookingApi/BookingApi.csproj"
COPY . .
WORKDIR "/src/BookingApi"
RUN dotnet build "BookingApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingApi.dll"]
