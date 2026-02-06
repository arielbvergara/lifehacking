#!/bin/bash

# Start Firebase Firestore emulator for testing
echo "Starting Firestore emulator..."

# Set environment variable for tests
export FIRESTORE_EMULATOR_HOST="127.0.0.1:8080"

# Start emulator in background using consistent project ID
firebase emulators:start --only firestore --project demo-test &

# Wait for emulator to be ready
echo "Waiting for emulator to start..."
sleep 8

# Check if emulator is running
if curl -s http://127.0.0.1:8080 > /dev/null; then
    echo "✅ Firestore emulator is running on http://127.0.0.1:8080"
    echo "🌐 Emulator UI available at http://127.0.0.1:4000"
else
    echo "❌ Failed to start Firestore emulator"
    exit 1
fi