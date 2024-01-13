# Use the official Microsoft .NET SDK image.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Set the working directory inside the container.
WORKDIR /app

# Copy the .csproj file and restore any dependencies (via 'dotnet restore').
COPY *.csproj ./
RUN dotnet restore

# Copy the project files and build the release.
COPY . ./
RUN dotnet publish -c Release -o out

# Generate the runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Set the environment variables from the .env file.
# This is just a placeholder; the actual variables will be supplied at runtime.
ENV IoTHub__IoTHubName=
ENV IoTHub__DeviceId=
ENV IoTHub__Key=
ENV FILE_UPLOAD_PATH=

# Start the application.
ENTRYPOINT ["dotnet", "iot_sensor.dll"]
