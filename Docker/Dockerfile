FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NuReaper.Api/NuReaper.Api.csproj", "NuReaper.Api/"]
COPY ["NuReaper.Application/NuReaper.Application.csproj", "NuReaper.Application/"]
COPY ["NuReaper.Domain/NuReaper.Domain.csproj", "NuReaper.Domain/"]
COPY ["NuReaper.Infrastructure/NuReaper.Infrastructure.csproj", "NuReaper.Infrastructure/"]
RUN dotnet restore "NuReaper.Api/NuReaper.Api.csproj"
COPY . .
WORKDIR "/src/NuReaper.Api"
RUN dotnet build "NuReaper.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NuReaper.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NuReaper.Api.dll"]
