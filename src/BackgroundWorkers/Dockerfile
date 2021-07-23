FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["BackgroundWorkers/BackgroundWorkers.csproj", "BackgroundWorkers/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Components/Components.csproj", "Components/"]
RUN dotnet restore "Contracts/Contracts.csproj" \
  && dotnet restore "Infrastructure/Infrastructure.csproj" \
  && dotnet restore "Components/Components.csproj" \
  && dotnet restore "BackgroundWorkers/BackgroundWorkers.csproj"

COPY . .
RUN dotnet publish -c Release -o /app "BackgroundWorkers/BackgroundWorkers.csproj"

FROM runtime-image AS publish
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "EverythingMessages.BackgroundWorkers.dll"]
