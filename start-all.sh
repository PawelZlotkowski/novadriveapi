#!/bin/bash

echo "Starting NovaDrive Application Stack..."
echo ""

# Must be run from the project root (where compose.yaml lives)
cd "$(dirname "$0")"

if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "Docker is running"
echo ""
echo "Building and starting all services..."
echo "This may take a few minutes on first run..."
echo ""

docker compose up --build

# Use Ctrl+C to stop all services
