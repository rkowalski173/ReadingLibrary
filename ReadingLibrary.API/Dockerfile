FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["ReadingLibrary.API/ReadingLibrary.API.csproj", "ReadingLibrary.API/"]
COPY ["ReadingLibrary.Clients/ReadingLibrary.Clients.csproj", "ReadingLibrary.Clients/"]
COPY ["ReadingLibrary.Contracts/ReadingLibrary.Contracts.csproj", "ReadingLibrary.Contracts/"]
COPY ["ReadingLibrary/ReadingLibrary.csproj", "ReadingLibrary/"]
RUN dotnet restore "ReadingLibrary.API/ReadingLibrary.API.csproj"
COPY . .
WORKDIR "/src/ReadingLibrary.API"
RUN dotnet build "./ReadingLibrary.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReadingLibrary.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReadingLibrary.API.dll"]
