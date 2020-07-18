FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY nuget.config src/
COPY ["src/StaplePuck.Calculator/StaplePuck.Calculator.csproj", "src/StaplePuck.Calculator/"]
RUN dotnet restore "src/StaplePuck.Calculator/StaplePuck.Calculator.csproj"
COPY . .
WORKDIR "/src/src/StaplePuck.Calculator"
RUN dotnet build "StaplePuck.Calculator.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "StaplePuck.Calculator.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "StaplePuck.Calculator.dll"]
