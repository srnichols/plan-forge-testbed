FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["src/TimeTracker.Api/TimeTracker.Api.csproj", "src/TimeTracker.Api/"]
COPY ["src/TimeTracker.Core/TimeTracker.Core.csproj", "src/TimeTracker.Core/"]
RUN dotnet restore "src/TimeTracker.Api/TimeTracker.Api.csproj"
COPY . .
RUN dotnet build "src/TimeTracker.Api/TimeTracker.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/TimeTracker.Api/TimeTracker.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TimeTracker.Api.dll"]
