#!/bin/bash

# Integration Test Script for OpenTelemetry PostgreSQL Exporters
# This script starts a PostgreSQL container and runs integration tests

echo "🐘 Starting PostgreSQL container..."
docker-compose up -d

echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 10

# Check if PostgreSQL is ready
docker-compose exec postgres pg_isready -U user -d testdb
if [ $? -ne 0 ]; then
    echo "❌ PostgreSQL is not ready. Waiting a bit more..."
    sleep 5
    docker-compose exec postgres pg_isready -U user -d testdb
    if [ $? -ne 0 ]; then
        echo "❌ PostgreSQL failed to start properly. Exiting."
        docker-compose down
        exit 1
    fi
fi

echo "✅ PostgreSQL is ready!"

echo "🧪 Running integration tests..."
echo "Note: You need to manually enable the tests by removing the Skip parameter"
echo "from the [Fact(Skip = \"..\")] attributes in ManualIntegrationTests.cs"

# Show which tests would run
dotnet test --filter "ManualIntegrationTests" --list-tests

echo ""
echo "📋 To run the integration tests:"
echo "1. Edit Int31h.Common.OpenTelemetry.PostgresExporters.Tests/ManualIntegrationTests.cs"
echo "2. Remove 'Skip = \"Manual integration test...\"' from the [Fact] attributes"
echo "3. Run: dotnet test --filter \"ManualIntegrationTests\""
echo ""
echo "🔗 PostgreSQL Connection:"
echo "Host: localhost"
echo "Port: 5432"
echo "Database: testdb"
echo "Username: user"
echo "Password: password"
echo ""
echo "🛑 To stop PostgreSQL container: docker-compose down"

echo ""
echo "🎯 Example: Running a single test manually after removing Skip:"
echo "dotnet test --filter \"TraceExporter_WithRealDatabase_CreatesTablesAndExportsData\""