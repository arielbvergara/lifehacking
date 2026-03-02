#!/bin/bash
# Start Firebase Firestore emulator with test configuration
# This uses firestore.test.rules which allows all operations for testing

echo "Starting Firestore emulator with test configuration..."
echo "Using firebase.test.json (permissive rules for testing)"
echo ""

firebase emulators:start --only firestore --config firebase.test.json
