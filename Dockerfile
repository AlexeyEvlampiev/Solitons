# Base build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build


# Set working directory
WORKDIR /app

# Copy solution file
COPY solitons.sln ./
COPY Solitons.png ./

# Copy source and test projects
COPY src/ src/
COPY test/ test/

# Restore dependencies
RUN dotnet restore solitons.sln

# Build solution
RUN dotnet build solitons.sln -c Release --no-restore

# Run tests
ARG SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING
ENV SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING=${SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING}
RUN dotnet test solitons.sln -c Release --no-build --verbosity normal

# Create NuGet packages
RUN dotnet pack src/Solitons.Core/Solitons.Core.csproj -c Release --no-build -o /app/packages && \
    dotnet pack src/Solitons.Postgres/Solitons.Postgres.csproj -c Release --no-build -o /app/packages && \
    dotnet pack src/Solitons.Postgres.PgUp/Solitons.Postgres.PgUp.csproj -c Release --no-build -o /app/packages && \
    dotnet pack src/Solitons.SQLite/Solitons.SQLite.csproj -c Release --no-build -o /app/packages && \
    dotnet pack src/Solitons.Azure/Solitons.Azure.csproj -c Release --no-build -o /app/packages

# Final stage to hold the packages
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /packages
COPY --from=build /app/packages .