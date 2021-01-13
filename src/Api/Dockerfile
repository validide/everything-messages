FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Api/Api.csproj", "Api/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "Api/Api.csproj"

COPY . .
RUN dotnet publish -c Release -o /app "Api/Api.csproj"

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS publish
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "EverythingMessages.Api.dll"]
