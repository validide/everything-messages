FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Scheduler/Scheduler.csproj", "Scheduler/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "Infrastructure/Infrastructure.csproj" \
  && dotnet restore "Scheduler/Scheduler.csproj"

COPY . .
RUN dotnet publish -c Release -o /app "Scheduler/Scheduler.csproj"

FROM em-dotnet-runtime-image AS publish
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "EverythingMessages.Scheduler.dll"]
