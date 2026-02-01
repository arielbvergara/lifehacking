# Infrastructure.Tests – Firestore Emulator Guide

This test project includes integration-style tests for the Firestore-backed `IUserRepository` implementation (`FirestoreUserRepository`). These tests are designed to **work against the Firebase Firestore emulator**, and are **no-op** when the emulator is not configured.

## When the Firestore tests run

Tests in `FirestoreUserRepositoryTests` call `TryCreateRepository`. They only execute their assertions when the `FIRESTORE_EMULATOR_HOST` environment variable is set. Otherwise, each test returns early so the suite can run without Firestore.

## Prerequisites

- Firebase CLI installed (`firebase` command available).
- A Firebase project (can be a throwaway/test project).
- Firestore emulator enabled in your local Firebase configuration.

## Running the Firestore emulator

From the directory where your `firebase.json` is located (or any directory with a suitable Firebase config):

```bash
firebase emulators:start --only firestore
```

This command will start the Firestore emulator on a host/port (commonly `localhost:8080`). The exact host/port is what you pass via `FIRESTORE_EMULATOR_HOST`.

## Environment variables

Before running tests, set the emulator host environment variable so that both the tests and the application code direct Firestore traffic to the emulator:

```bash
export FIRESTORE_EMULATOR_HOST=localhost:8080
```

- The tests look for `FIRESTORE_EMULATOR_HOST` and, if present, create a `FirestoreDb` instance for the test project id (`lifehacking-test`).
- The `FirebaseDatabaseOptions` used in tests specify a separate users collection (`users-tests`) so that test data is isolated from any real collections.

## Running the tests

From the repository root:

```bash
# Ensure the emulator is running and FIRESTORE_EMULATOR_HOST is set
export FIRESTORE_EMULATOR_HOST=localhost:8080

dotnet test lifehacking/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj
```

If the emulator is running and `FIRESTORE_EMULATOR_HOST` is set, the `FirestoreUserRepositoryTests` will:

- Persist users into the `users-tests` collection in the emulator.
- Verify that `AddAsync` and `GetByIdAsync` work end-to-end.
- Verify that `GetPagedAsync` applies filtering and paging consistent with the in-memory/EF Core implementation.

If the emulator is **not** configured, these tests effectively skip their assertions, and the rest of the Infrastructure tests still run normally.