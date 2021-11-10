FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["BackgroundAuditor/BackgroundAuditor.csproj", "BackgroundAuditor/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Components/Components.csproj", "Components/"]
RUN dotnet restore "Contracts/Contracts.csproj" \
  && dotnet restore "Infrastructure/Infrastructure.csproj" \
  && dotnet restore "Components/Components.csproj" \
  && dotnet restore "BackgroundAuditor/BackgroundAuditor.csproj"

COPY . .
RUN dotnet publish -c Release -o /app "BackgroundAuditor/BackgroundAuditor.csproj"

FROM em-dotnet-runtime-image AS publish
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "EverythingMessages.BackgroundAuditor.dll"]
