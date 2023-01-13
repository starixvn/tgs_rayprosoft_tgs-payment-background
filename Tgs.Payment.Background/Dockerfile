#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.Background/Tgs.Payment.Background.csproj", "Tgs.Payment.Background/"]
COPY ["Payment.Background.Core/Payment.Background.Core.csproj", "Payment.Background.Core/"]
COPY ["Payment.Background.Repositories/Payment.Background.Repositories.csproj", "Payment.Background.Repositories/"]
RUN dotnet restore "Tgs.Payment.Background/Tgs.Payment.Background.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.Background"
RUN dotnet build "Tgs.Payment.Background.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Payment.Background.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.Background.dll"]