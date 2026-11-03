FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Domain/ECommerce.Domain.csproj ECommerce.Domain/
COPY ECommerce.Application/ECommerce.Application.csproj ECommerce.Application/
COPY ECommerce.Infrastructure/ECommerce.Infrastructure.csproj ECommerce.Infrastructure/
COPY ECommerce.Api/ECommerce.Api.csproj ECommerce.Api/
RUN dotnet restore ECommerce.Api/ECommerce.Api.csproj
COPY ECommerce.Domain/ ECommerce.Domain/
COPY ECommerce.Application/ ECommerce.Application/
COPY ECommerce.Infrastructure/ ECommerce.Infrastructure/
COPY ECommerce.Api/ ECommerce.Api/
RUN dotnet publish ECommerce.Api/ECommerce.Api.csproj -c Release -o /out --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
