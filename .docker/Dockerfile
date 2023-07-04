FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /App

# add inkscape
RUN apt-get update \
    && apt-get install -y inkscape

COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Palantir.dll"]