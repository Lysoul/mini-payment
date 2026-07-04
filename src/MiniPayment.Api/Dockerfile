FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["src/MiniPayment.Api/MiniPayment.Api.csproj", "src/MiniPayment.Api/"]
COPY ["src/MiniPayment.Application/MiniPayment.Application.csproj", "src/MiniPayment.Application/"]
COPY ["src/MiniPayment.Domain/MiniPayment.Domain.csproj", "src/MiniPayment.Domain/"]
COPY ["src/MiniPayment.Infrastructure/MiniPayment.Infrastructure.csproj", "src/MiniPayment.Infrastructure/"]

RUN dotnet restore "src/MiniPayment.Api/MiniPayment.Api.csproj"

COPY . .
WORKDIR "/src/src/MiniPayment.Api"
RUN dotnet build "MiniPayment.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MiniPayment.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MiniPayment.Api.dll"]
