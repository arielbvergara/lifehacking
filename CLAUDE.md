# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All commands run from the repository root (where `lifehacking.slnx` lives).

```bash
# Build
dotnet build lifehacking.slnx

# Run API (dev mode with Swagger UI)
dotnet run --project lifehacking/WebAPI/WebAPI.csproj

# Run all tests
dotnet test lifehacking.slnx

# Run a single test project
dotnet test lifehacking/Tests/Application.Tests/Application.Tests.csproj
dotnet test lifehacking/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj
dotnet test lifehacking/Tests/WebAPI.Tests/WebAPI.Tests.csproj

# Run a single test method
dotnet test lifehacking/Tests/Application.Tests/Application.Tests.csproj --filter "Name=<MethodName>"

# Run all tests in a class
dotnet test lifehacking/Tests/Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~<ClassName>"

# Format check (CI enforces this)
dotnet format --verify-no-changes

# Docker (full stack with PostgreSQL)
docker compose up --build
```

## Architecture

.NET 10 Clean Architecture solution (`lifehacking.slnx`) with strict unidirectional dependencies:

```
Domain → Application → Infrastructure → WebAPI
```

- **Domain** (`lifehacking/Domain/`) — Entities, value objects, `Result<T, TE>` primitive. Zero external dependencies.
- **Application** (`lifehacking/Application/`) — Use cases (grouped by feature), DTOs, repository interfaces (ports), `AppException` hierarchy, `CacheKeys`, `FileValidationHelper`.
- **Infrastructure** (`lifehacking/Infrastructure/`) — EF Core + PostgreSQL (primary), AWS S3/CloudFront for images, plus Firestore document classes (legacy/coexisting).
- **WebAPI** (`lifehacking/WebAPI/`) — ASP.NET Core 10, thin controllers, `GlobalExceptionFilter`, Firebase JWT auth, rate limiting, security headers.
- **Tests** (`lifehacking/Tests/`) — `Application.Tests`, `Infrastructure.Tests`, `WebAPI.Tests`. All use xUnit + Microsoft.Testing.Platform + FluentAssertions + Moq.

HTTP flow: Controller → Use case → Domain + Infrastructure → `Result<T, AppException>` → Controller maps to HTTP response.

## Key Patterns and Constraints

**Result pattern over exceptions**: Use cases return `Result<TSuccess, AppException>`. Controllers call `.Match(ok => Ok(ok), err => err.ToActionResult())`. Never throw for control flow.

**Caching lives in use cases**: Use `IMemoryCache` with keys from `Application.Caching.CacheKeys`. Cache durations are constants defined in the use case (categories: 1 hour, dashboard: 1 hour). Controllers must stay thin.

**No magic values**: All constants go in `ImageConstants`, `CacheKeys`, or named constants in the relevant layer.

**Soft delete**: `User`, `Tip`, `Category` have `DeletedAt`/`IsDeleted`; category deletion cascades to tips.

**Image upload security**: Magic byte validation, filename sanitization, MIME allowlist (JPEG/PNG/GIF/WebP), 5 MB max — all in `FileValidationHelper`.

**Domain isolation**: Keep `Domain` free of Firestore, Firebase, ASP.NET, or AWS references.

**Testing**:
- Naming: `{MethodName}_Should{DoSomething}_When{Condition}`
- Use real `IMemoryCache` (not mocked) when testing caching behavior
- Prefer `obj.Should().Be(...)` (FluentAssertions) over `Assert.*`

## Git and Branching

- Branches: `issue-<id>-<kebab-description>`
- Commits: Conventional Commits (`feat`, `fix`, `chore`, `refactor`, `docs`, `test`) in imperative mood
- Include ticket ID in commit footer
- No direct commits to `master`/`main`; PRs required
- Architecture changes require a new ADR in `ADRs/` (numbered, zero-padded, based on most recent ADR style)

## MCP Server Priority

1. **microsoft.docs.mcp** — .NET, C#, ASP.NET, Azure, Microsoft APIs
2. **github** — repository source, implementations
3. **DeepWiki** — high-level overviews

Do not guess when factual accuracy is required — use MCP. If MCP fails, say so explicitly.
