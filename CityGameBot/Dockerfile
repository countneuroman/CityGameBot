FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CityGameBot/CityGameBot.csproj", "CityGameBot/"]
RUN dotnet restore "CityGameBot/CityGameBot.csproj"
COPY . .
WORKDIR "/src/CityGameBot"
RUN dotnet build "CityGameBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CityGameBot.csproj" -c Release -o /app/publish /p:UseAppHost=false
COPY ["CityGameBot/Cities.json", "/app/publish/Cities.json"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CityGameBot.dll"]
