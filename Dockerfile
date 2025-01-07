# Base build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Define build arguments
ARG STAGING_TYPE=Alpha
ARG SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING
ARG NUGET_API_KEY

# Set environment variables
ENV SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING=${SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING}
ENV NUGET_API_KEY=${NUGET_API_KEY}

# Set working directory
WORKDIR /app

# Copy solution file
COPY solitons.sln ./
COPY Solitons.png ./

# Copy source and test projects
COPY src/ src/
COPY test/ test/
COPY build/ build/

RUN pwsh -Command ". ./build/build.ps1"


# Final stage to hold the packages
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /packages
COPY --from=build /app/packages .