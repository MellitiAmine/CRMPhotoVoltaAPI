# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CrmPhotoVoltaApis.sln ./
COPY CrmPhotoVoltaApis.csproj ./
COPY CrmPhotoVolta.Domain/CrmPhotoVolta.Domain.csproj CrmPhotoVolta.Domain/
COPY CrmPhotoVolta.Application/CrmPhotoVolta.Application.csproj CrmPhotoVolta.Application/
COPY CrmPhotoVolta.Infrastructure/CrmPhotoVolta.Infrastructure.csproj CrmPhotoVolta.Infrastructure/

RUN dotnet restore CrmPhotoVoltaApis.csproj

COPY . .
RUN dotnet publish CrmPhotoVoltaApis.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Render sets PORT at runtime; default for local `docker run`.
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["/bin/sh", "-c", "export ASPNETCORE_URLS=\"http://+:${PORT}\" && exec dotnet CrmPhotoVoltaApis.dll"]
