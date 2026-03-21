FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "TutorPlatform.csproj"
RUN dotnet publish "TutorPlatform.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DatabaseProvider=Sqlite
ENV ConnectionStrings__SqliteConnection=Data Source=/var/data/tutor-platform.db
EXPOSE 10000
ENTRYPOINT ["dotnet", "TutorPlatform.dll"]
