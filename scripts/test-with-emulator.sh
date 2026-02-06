#!/bin/bash

# Run tests with Firestore emulator
set -e

echo "🚀 Starting Firestore emulator and running tests..."

# Set environment variable for tests
export FIRESTORE_EMULATOR_HOST="127.0.0.1:8080"

# Start emulator in background using consistent project ID
echo "Starting Firestore emulator..."
firebase emulators:start --only firestore --project demo-test &
EMULATOR_PID=$!

# Wait for emulator to be ready
echo "Waiting for emulator to start..."
sleep 8

# Function to cleanup on exit
cleanup() {
    echo "Stopping emulator..."
    kill $EMULATOR_PID 2>/dev/null || true
    pkill -f "firebase emulators:start" 2>/dev/null || true
    pkill -f "java.*firestore" 2>/dev/null || true
}

# Set trap to cleanup on script exit
trap cleanup EXIT

# Check if emulator is running
if ! curl -s http://127.0.0.1:8080 > /dev/null; then
    echo "❌ Failed to start Firestore emulator"
    exit 1
fi

echo "✅ Firestore emulator is running"

# Run tests
echo "🧪 Running tests..."
dotnet test lifehacking.slnx --logger "console;verbosity=normal"

echo "✅ Tests completed"