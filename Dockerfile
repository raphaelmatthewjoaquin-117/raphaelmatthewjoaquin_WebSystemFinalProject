FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["StudentGradeManagement/StudentGradeManagement.csproj", "StudentGradeManagement/"]
RUN dotnet restore "StudentGradeManagement/StudentGradeManagement.csproj"
COPY . .
WORKDIR /src/StudentGradeManagement
RUN dotnet publish "StudentGradeManagement.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StudentGradeManagement.dll"]
