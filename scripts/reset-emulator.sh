#!/bin/bash

# Reset Firestore emulator state by clearing all data
set -e

echo "🧹 Resetting Firestore emulator state..."

# Check if emulator is running
if ! curl -s http://127.0.0.1:8080 > /dev/null; then
    echo "❌ Firestore emulator is not running on port 8080"
    echo "💡 Start the emulator first with: ./scripts/start-emulator.sh"
    exit 1
fi

# Clear emulator data using the REST API
echo "🗑️  Clearing all emulator data..."
curl -X DELETE "http://127.0.0.1:8080/emulator/v1/projects/demo-test/databases/(default)/documents" \
  -H "Content-Type: application/json" \
  || echo "⚠️  Warning: Failed to clear some data (this might be normal)"

echo "✅ Emulator state reset complete"
echo "🌐 Emulator UI: http://127.0.0.1:4000"