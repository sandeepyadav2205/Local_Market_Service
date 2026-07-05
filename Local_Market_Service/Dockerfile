FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Local_Market_Service.csproj", "./"]
RUN dotnet restore "./Local_Market_Service.csproj"

# Copy everything else and publish
COPY . .
RUN dotnet publish "Local_Market_Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Local_Market_Service.dll"]
