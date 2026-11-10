FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SofiaInfis.Api/ SofiaInfis.Api/
COPY SofiaInfis.Web/ SofiaInfis.Web/
RUN dotnet publish SofiaInfis.Web/SofiaInfis.Web.csproj -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "SofiaInfis.Web.dll"]
