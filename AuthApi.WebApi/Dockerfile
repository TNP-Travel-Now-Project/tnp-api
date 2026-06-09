FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AuthApi.WebApi/AuthApi.WebApi.csproj", "AuthApi.WebApi/"]
COPY ["AuthApi.Application/AuthApi.Application.csproj", "AuthApi.Application/"]
COPY ["AuthApi.Domain/AuthApi.Domain.csproj", "AuthApi.Domain/"]
COPY ["AuthApi.Infrastructure/AuthApi.Infrastructure.csproj", "AuthApi.Infrastructure/"]
RUN dotnet restore "AuthApi.WebApi/AuthApi.WebApi.csproj"

COPY . .
RUN dotnet publish "AuthApi.WebApi/AuthApi.WebApi.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app .

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
