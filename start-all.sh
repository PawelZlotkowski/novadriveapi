#!/bin/bash

echo "🚀 Starting NovaDrive Application Stack..."
echo ""

# Navigate to src directory
cd src

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "✅ Docker is running"
echo ""
echo "Building and starting all services..."
echo "This may take a few minutes on first run..."
echo ""

# Start all services
docker-compose up --build

# Note: Use Ctrl+C to stop all services
