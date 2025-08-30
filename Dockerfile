FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "BE_OPENSKY/BE_OPENSKY.csproj"
WORKDIR "/src/BE_OPENSKY"
RUN dotnet build "BE_OPENSKY.csproj" -c Release -o /app/build
RUN dotnet publish "BE_OPENSKY.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BE_OPENSKY.dll"]
