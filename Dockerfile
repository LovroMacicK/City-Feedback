# ================================
# 1. OSNOVNA SLIKA (.NET runtime)
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# ================================
# 2. BUILD SLIKA (.NET SDK)
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiramo .csproj datoteko
COPY ["City-Feedback.csproj", "."]

# Obnovimo vse odvisnosti
RUN dotnet restore "City-Feedback.csproj"

# Kopiramo preostalo vsebino projekta
COPY . .

# Zgradimo projekt
RUN dotnet build "City-Feedback.csproj" -c Release -o /app/build

# ================================
# 3. OBJAVA (publish)
# ================================
FROM build AS publish
RUN dotnet publish "City-Feedback.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ================================
# 4. KONČNA SLIKA
# ================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "City-Feedback.dll"]
