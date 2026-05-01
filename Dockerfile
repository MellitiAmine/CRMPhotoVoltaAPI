# CRM PhotoVolta API — .NET 8 + PostgreSQL (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CrmPhotoVoltaApis.csproj .
COPY CrmPhotoVolta.Application/CrmPhotoVolta.Application.csproj CrmPhotoVolta.Application/
COPY CrmPhotoVolta.Domain/CrmPhotoVolta.Domain.csproj CrmPhotoVolta.Domain/
COPY CrmPhotoVolta.Infrastructure/CrmPhotoVolta.Infrastructure.csproj CrmPhotoVolta.Infrastructure/

RUN dotnet restore CrmPhotoVoltaApis.csproj

COPY CrmPhotoVolta.Application/ CrmPhotoVolta.Application/
COPY CrmPhotoVolta.Domain/ CrmPhotoVolta.Domain/
COPY CrmPhotoVolta.Infrastructure/ CrmPhotoVolta.Infrastructure/
COPY Controllers/ Controllers/
COPY Middleware/ Middleware/
COPY Properties/ Properties/
COPY Program.cs .
COPY appsettings.json appsettings.Development.json appsettings.Production.json ./

RUN dotnet publish CrmPhotoVoltaApis.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CrmPhotoVoltaApis.dll"]
