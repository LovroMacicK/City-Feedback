FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["City-Feedback.csproj", "."]

RUN dotnet restore "City-Feedback.csproj"

COPY . .

RUN dotnet build "City-Feedback.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "City-Feedback.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "City-Feedback.dll"]
