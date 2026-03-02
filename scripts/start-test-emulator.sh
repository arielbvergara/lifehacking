#!/bin/bash
# Start Firebase Firestore emulator with test configuration
# This uses firestore.test.rules which allows all operations for testing

FIRESTORE_EMULATOR_PROJECT="demo-test"
FIRESTORE_EMULATOR_HOST="127.0.0.1:8080"
export FIRESTORE_EMULATOR_HOST

echo "Starting Firestore emulator with test configuration..."
echo "Using firebase.test.json (permissive rules for testing)"
echo "Project: ${FIRESTORE_EMULATOR_PROJECT}"
echo "Firestore emulator host: ${FIRESTORE_EMULATOR_HOST}"
echo ""

firebase emulators:start --only firestore --config firebase.test.json --project "${FIRESTORE_EMULATOR_PROJECT}"
