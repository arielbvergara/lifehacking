#!/bin/bash

# Stop Firebase Firestore emulator
echo "Stopping Firestore emulator..."

# Kill emulator processes
pkill -f "firebase emulators:start"
pkill -f "java.*firestore"

echo "✅ Firestore emulator stopped"