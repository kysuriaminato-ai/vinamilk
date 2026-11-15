FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Vinamilk.HRM.sln", "./"]
COPY ["src/Vinamilk.HRM.Domain/Vinamilk.HRM.Domain.csproj", "src/Vinamilk.HRM.Domain/"]
COPY ["src/Vinamilk.HRM.Application/Vinamilk.HRM.Application.csproj", "src/Vinamilk.HRM.Application/"]
COPY ["src/Vinamilk.HRM.Infrastructure/Vinamilk.HRM.Infrastructure.csproj", "src/Vinamilk.HRM.Infrastructure/"]
COPY ["src/Vinamilk.HRM.Web/Vinamilk.HRM.Web.csproj", "src/Vinamilk.HRM.Web/"]
RUN dotnet restore "src/Vinamilk.HRM.Web/Vinamilk.HRM.Web.csproj"

COPY . .
RUN dotnet publish "src/Vinamilk.HRM.Web/Vinamilk.HRM.Web.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /app/logs /app/backups
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vinamilk.HRM.Web.dll"]

