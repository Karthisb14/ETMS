# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Etms.slnx .
COPY Etms.Web/Etms.Web.csproj Etms.Web/
COPY Etms.Domain/Etms.Domain.csproj Etms.Domain/
COPY Etms.Data/Etms.Data.csproj Etms.Data/
COPY Etms.Tests/Etms.Tests.csproj Etms.Tests/
RUN dotnet restore Etms.Web/Etms.Web.csproj

COPY . .
RUN dotnet publish Etms.Web/Etms.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user (OWASP: least privilege for the container process).
RUN adduser --disabled-password --gecos "" etms
USER etms

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Etms.Web.dll"]
