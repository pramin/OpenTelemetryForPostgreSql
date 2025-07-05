#!/bin/bash

# Sample Application Runner Script
# This script starts PostgreSQL and runs the sample application

set -e

echo "🐳 Starting PostgreSQL container..."
docker-compose up -d

echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 5

# Test PostgreSQL connection
echo "🔍 Testing PostgreSQL connection..."
if docker exec -it opentelemetryforpostgresql-db-1 pg_isready -U user -d testdb > /dev/null 2>&1; then
    echo "✅ PostgreSQL is ready!"
else
    echo "❌ PostgreSQL is not ready. Waiting a bit more..."
    sleep 10
    if docker exec -it opentelemetryforpostgresql-db-1 pg_isready -U user -d testdb > /dev/null 2>&1; then
        echo "✅ PostgreSQL is ready now!"
    else
        echo "❌ PostgreSQL failed to start. Check docker-compose logs."
        exit 1
    fi
fi

echo "🚀 Running sample application..."
cd SampleApplication
dotnet run

echo "🗄️  To explore the data in PostgreSQL, connect with:"
echo "   Host: localhost"
echo "   Port: 5432"
echo "   Database: testdb"
echo "   Username: user"
echo "   Password: password"
echo ""
echo "📊 To stop PostgreSQL: docker-compose down"