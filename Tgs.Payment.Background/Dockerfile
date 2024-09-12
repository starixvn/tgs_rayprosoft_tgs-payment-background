FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.Background/Tgs.Payment.Background.csproj", "Tgs.Payment.Background/"]
COPY ["Payment.Background.Core/Payment.Background.Core.csproj", "Payment.Background.Core/"]
COPY ["Payment.Background.Repositories/Payment.Background.Repositories.csproj", "Payment.Background.Repositories/"]
RUN dotnet restore "./Tgs.Payment.Background/./Tgs.Payment.Background.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.Background"
RUN dotnet build "./Tgs.Payment.Background.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.Background.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.Background.dll"]