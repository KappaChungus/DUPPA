# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published output
COPY --from=build /app/out .

# !!! ALSO copy the database file manually !!!
COPY ./Database/duppa.db ./Database/duppa.db

# Set the environment port (important for Render)
ENV ASPNETCORE_URLS=http://+:$PORT

# Start the app
ENTRYPOINT ["dotnet", "DUPPA.dll"]
