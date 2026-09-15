FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/UTH.Library.Domain/UTH.Library.Domain.csproj", "src/UTH.Library.Domain/"]
COPY ["src/UTH.Library.Application/UTH.Library.Application.csproj", "src/UTH.Library.Application/"]
COPY ["src/UTH.Library.Infrastructure/UTH.Library.Infrastructure.csproj", "src/UTH.Library.Infrastructure/"]
COPY ["src/UTH.Library.Api/UTH.Library.Api.csproj", "src/UTH.Library.Api/"]
RUN dotnet restore "src/UTH.Library.Api/UTH.Library.Api.csproj"
COPY src src
RUN dotnet publish "src/UTH.Library.Api/UTH.Library.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UTH.Library.Api.dll"]