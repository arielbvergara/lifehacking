# Lifehacking Tips API

A production-ready REST API for building tip discovery and management applications. Lifehacking provides a complete backend for browsing practical daily-life tips, organizing them by categories, and managing user favorites with seamless anonymous-to-authenticated user transitions.

Built with **.NET 10** and **Clean Architecture** principles. This project was bootstrapped from [arielbvergara/clean-architecture](https://github.com/arielbvergara/clean-architecture) — a reusable Clean Architecture template developed during lessons and then made into a template.

> 🤖 AI-assisted development: [**Kiro**](https://kiro.dev) was used as an AI assistant throughout the development of this project.

---

## 🔗 Related Projects

| Project | Description | Deployment |
|---------|-------------|------------|
| **[lifehacking-app](https://github.com/arielbvergara/lifehacking-app)** | Frontend — Next.js 16, Google Stitch design, Firebase, Docker, Vercel | [Vercel](https://vercel.com) |
| **lifehacking** *(this repo)* | Backend API — .NET 10, Clean Architecture, Firebase, Docker, AWS | [Koyeb](https://app.koyeb.com) |

---

## 🛠️ Tech Stack

### Backend (this repository)

| Technology | Purpose |
|-----------|---------|
| **.NET 10 + Clean Architecture** | Web API with Domain, Application, Infrastructure, and WebAPI layers |
| **Firebase Authentication** | JWT Bearer token validation and identity management |
| **Firebase Cloud Firestore** | Primary NoSQL database |
| **AWS S3** | Category image storage |
| **AWS CloudFront** | CDN for delivering images |
| **Docker & Docker Compose** | Containerised deployment |
| **[Koyeb](https://app.koyeb.com)** | Cloud deployment platform |
| **Dependabot** | Automated weekly dependency updates |
| **GitHub Actions** | CI pipeline (build, test, lint, security scanning) and code review |
| **Kiro** | AI assistant used during development |
| **Sentry** | Error tracking and performance monitoring |
| **Swagger / OpenAPI** | Interactive API documentation |

### Frontend ([lifehacking-app](https://github.com/arielbvergara/lifehacking-app))

| Technology | Purpose |
|-----------|---------|
| **Next.js 16** | React-based frontend framework |
| **Google Stitch** | UI/UX design |
| **Firebase Authentication** | Authentication and identity |
| **Vercel** | Frontend deployment |
| **Sentry.io** | Monitoring and error tracking |
| **Docker** | Containerised deployment |
| **Dependabot** | Automated weekly dependency updates |
| **GitHub Actions** | CI pipeline and code review |
| **Kiro** | AI assistant used during development |
| **Github Copilot** | AI assistant used for code review |

---

## Overview

Lifehacking Tips API enables you to build applications where users can:

- **Discover tips** through search, filtering, and category browsing (no authentication required)
- **Save favorites** with automatic sync between local storage and server-side persistence
- **Manage content** through a complete admin interface for tips and categories
- **Handle users** with Firebase authentication, role-based access, and self-service account management

The API supports three user types:
- **Anonymous users** - Full read access with client-side favorites
- **Authenticated users** - Persistent favorites with automatic local storage merge
- **Administrators** - Complete content and user management capabilities

## Features

### For Anonymous Users (Public API)

- Browse and search tips with advanced filtering (by category, tags, search term)
- View detailed tip information including step-by-step instructions
- Explore all available categories
- Sort results by creation date, update date, or title
- Paginated responses for optimal performance
- Client-side favorites management (local storage)

### For Authenticated Users

- All anonymous user capabilities
- Persistent favorites stored server-side
- Automatic merge of local favorites on first login (deduplicated)
- Cross-device favorite synchronization
- Self-service profile management (view, update name, delete account)

### For Administrators

- All authenticated user capabilities
- Complete tip lifecycle management (create, update, delete)
- Category management with cascade delete
- User management and administration
- Admin user creation with Firebase integration
- Audit logging for all administrative actions

## API Endpoints

All endpoints return JSON and follow RFC 7807 Problem Details for error responses. Each response includes a `correlationId` for request tracing.

For complete request/response schemas, validation rules, and interactive testing, see the **Swagger UI** at `http://localhost:8080/swagger` when running the API.

### Public Endpoints (No Authentication Required)

#### Tips API - `/api/tip`

- `GET /api/tip` - Search and filter tips
  - Query parameters: `q` (search term), `categoryId`, `tags[]`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Returns paginated tip summaries with metadata
- `GET /api/tip/{id}` - Get complete tip details
  - Returns full tip with title, description, ordered steps, category, tags, and optional video URL

#### Categories API - `/api/category`

- `GET /api/category` - List all available categories
  - Returns all non-deleted categories
- `GET /api/category/{id}/tips` - Get tips by category
  - Query parameters: `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Returns paginated tips for the specified category

### Authenticated Endpoints (Requires JWT Bearer Token)

#### User API - `/api/user`

- `POST /api/user` - Create user profile after authentication
  - Called once after Firebase authentication to create internal user record
  - External auth ID derived from JWT token
- `GET /api/user/me` - Get current user profile
  - User resolved from JWT token
- `PUT /api/user/me/name` - Update current user's display name
  - Self-service profile update
- `DELETE /api/user/me` - Delete current user account
  - Soft-delete with audit trail

#### Favorites API - `/api/me/favorites`

- `GET /api/me/favorites` - List user's favorite tips
  - Query parameters: `q`, `categoryId`, `tags[]`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Returns paginated favorites with full tip details
- `POST /api/me/favorites/{tipId}` - Add tip to favorites
  - Idempotent operation
- `DELETE /api/me/favorites/{tipId}` - Remove tip from favorites
- `POST /api/me/favorites/merge` - Merge local favorites from client storage
  - Accepts array of tip IDs from local storage
  - Returns summary with added, skipped, and failed counts
  - Idempotent and supports partial success

### Admin Endpoints (Requires Admin Role)

#### Admin Tips API - `/api/admin/tips`

- `POST /api/admin/tips` - Create new tip
  - Requires: title, description, steps (ordered list), categoryId
  - Optional: tags (max 10), videoUrl (YouTube/Instagram)
- `PUT /api/admin/tips/{id}` - Update existing tip
  - All fields updatable
- `DELETE /api/admin/tips/{id}` - Soft-delete tip
  - Marks tip as deleted, preserves data

#### Admin Categories API - `/api/admin/categories`

- `POST /api/admin/categories/images` - Upload category image
  - Accepts multipart/form-data with image file
  - Validates file size (max 5MB), content type (JPEG, PNG, GIF, WebP), and magic bytes
  - Uploads to AWS S3 with unique GUID-based filename
  - Returns image metadata including CloudFront CDN URL
  - Required for creating categories with images
- `POST /api/admin/categories` - Create new category
  - Requires: name (2-100 characters, unique case-insensitive)
  - Optional: image metadata from upload endpoint
- `PUT /api/admin/categories/{id}` - Update category name
  - Enforces uniqueness
- `DELETE /api/admin/categories/{id}` - Soft-delete category
  - Cascades soft-delete to all associated tips

#### Admin Users API - `/api/admin/user`

- `POST /api/admin/user` - Create admin user
  - Creates user in Firebase and internal database
  - Requires: email, displayName, password
- `GET /api/admin/user` - List users with pagination
  - Query parameters: `search`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`, `isDeleted`
  - Supports searching across email, name, and ID
- `GET /api/admin/user/{id}` - Get user by internal ID
- `GET /api/admin/user/email/{email}` - Get user by email address
- `PUT /api/admin/user/{id}/name` - Update user's display name
- `DELETE /api/admin/user/{id}` - Soft-delete user account

## Domain Model

### Tip
- **Title** (5-200 characters)
- **Description** (10-2000 characters)
- **Steps** (ordered list, each 10-500 characters)
- **Category** (single, required)
- **Tags** (optional, max 10, each 1-50 characters)
- **VideoUrl** (optional, YouTube or Instagram)
- Timestamps: CreatedAt, UpdatedAt, DeletedAt
- Soft-delete support

### Category
- **Name** (2-100 characters, unique case-insensitive)
- Timestamps: CreatedAt, UpdatedAt, DeletedAt
- Soft-delete support with cascade to tips

### User
- **Email** (unique)
- **Name** (display name)
- **ExternalAuthId** (Firebase UID)
- **Role** (User or Admin)
- Timestamps: CreatedAt, UpdatedAt, DeletedAt
- Soft-delete support

### UserFavorites
- **UserId** (reference to User)
- **TipId** (reference to Tip)
- **AddedAt** (timestamp)
- Composite key: UserId + TipId

## Technology Stack

See the [Tech Stack](#️-tech-stack) section above for a full overview of all tools and services used in this project.

- **.NET 10.0 Web API** - Modern C# with Clean Architecture (Domain, Application, Infrastructure, WebAPI layers)
- **Firebase Authentication** - JWT Bearer token validation with role-based authorization
- **Firebase Cloud Firestore** - Primary NoSQL datastore with real-time capabilities
- **AWS S3 + CloudFront** - Image storage and CDN delivery for category images
- **Sentry** - Error tracking, performance monitoring, and observability
- **Docker & Docker Compose** - Containerized deployment with single-command setup
- **Koyeb** - Cloud deployment platform (live at [app.koyeb.com](https://app.koyeb.com))
- **GitHub Actions** - CI/CD pipeline with automated build, test, lint, and security scanning
- **Dependabot** - Automated weekly dependency updates for NuGet, Docker, and GitHub Actions
- **Kiro** - AI assistant used throughout development
- **Microsoft Testing Platform** - Modern test runner with xUnit and property-based testing
- **Swagger/OpenAPI** - Interactive API documentation and testing

## Architecture

This API follows **Clean Architecture** principles with clear separation of concerns:

- **Domain Layer** - Core business entities and value objects (no external dependencies)
- **Application Layer** - Use cases, DTOs, and business logic orchestration
- **Infrastructure Layer** - Data access (Firestore), external services (Firebase Auth, Sentry)
- **WebAPI Layer** - Controllers, middleware, authentication/authorization, API contracts

Key architectural decisions are documented in `ADRs/` including:
- Firebase Firestore as the primary datastore (ADR-018)
- JWT/Firebase authentication with self-service `/me` endpoints
- User roles and soft-delete lifecycle (ADR-006)
- Security headers, rate limiting, and hardened production configuration (ADR-010, ADR-011)
- Sentry integration and standardized error handling (ADR-013, ADR-015)

For product requirements and MVP scope, see `docs/MVP.md`.

## Getting Started

### 1. Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/) or later
- [Docker](https://www.docker.com/) and Docker Compose
- A Firebase project for authentication (create one at [Firebase Console](https://console.firebase.google.com/))
- Optional: A Sentry project for monitoring (sign up at [sentry.io](https://sentry.io/))

### 2. Quick Start with Docker Compose

The fastest way to run the API locally with all dependencies configured:

```bash
docker compose up --build
```

This will:
- Build and start the WebAPI container
- Configure the API to use Firebase/Firestore
- Expose the API on port 8080

Once running:
- **API Base URL**: `http://localhost:8080`
- **Swagger UI**: `http://localhost:8080/swagger` (interactive API documentation)

Stop the services:

```bash
docker compose down
```

### 3. Run the WebAPI Directly (Without Docker)

For faster iteration during development, run the API directly using the .NET SDK:

```bash
# Build the solution
dotnet build lifehacking.slnx

# Run the WebAPI project
dotnet run --project lifehacking/WebAPI/WebAPI.csproj
```

The API reads configuration from `lifehacking/WebAPI/appsettings.Development.json` and environment variables. You can choose between:
- **In-memory database** (`UseInMemoryDB = true`) - Good for quick testing without Firebase setup
- **Firebase/Firestore** (`UseInMemoryDB = false`) - Production-like environment

### 4. Configure Firebase Authentication

To test authenticated and admin endpoints, configure Firebase as your identity provider:

1. **Update `appsettings.Development.json`**:
   ```json
   {
     "Authentication": {
       "Authority": "https://securetoken.google.com/<your-firebase-project-id>",
       "Audience": "<your-firebase-project-id>"
     },
     "Firebase": {
       "ProjectId": "<your-firebase-project-id>"
     }
   }
   ```

2. **Obtain a Firebase ID token**:
   - Authenticate a user through Firebase (web, mobile, or REST API)
   - Extract the ID token from the authentication response

3. **Use the token in API requests**:
   ```bash
   curl -H "Authorization: Bearer <firebase-id-token>" \
        http://localhost:8080/api/user/me
   ```

The API validates the JWT token and maps the `sub` claim to the internal user's `ExternalAuthId`.

### 5. Configure Sentry Monitoring (Optional)

Sentry integration is optional. The API runs normally with Sentry disabled.

To enable monitoring, set these environment variables:

```bash
export Sentry__Enabled=true
export Sentry__Dsn=<your-sentry-dsn>
export Sentry__Environment=Development
export Sentry__TracesSampleRate=0.2
```

Or configure in `appsettings.Development.json`:

```json
{
  "Sentry": {
    "Enabled": true,
    "Dsn": "<your-sentry-dsn>",
    "Environment": "Development",
    "TracesSampleRate": 0.2
  }
}
```

When enabled, unhandled errors and performance traces are sent to Sentry with full context (route, user, correlation ID).

### 6. Explore the API with Swagger

Once the API is running, navigate to the Swagger UI for interactive documentation:

**http://localhost:8080/swagger**

Swagger provides:
- Complete endpoint documentation with request/response schemas
- Validation rules and constraints
- Interactive testing (try out endpoints directly from the browser)
- Authentication support (add your Bearer token to test protected endpoints)

## Authentication & Authorization

### Authentication Flow

1. **User authenticates with Firebase** (your frontend handles this)
2. **Frontend receives Firebase ID token** (JWT)
3. **Frontend calls API with token** in `Authorization: Bearer <token>` header
4. **API validates token** with Firebase and extracts user identity
5. **API maps Firebase UID** to internal user record

### User Roles

- **Anonymous** - No authentication required, read-only access to tips and categories
- **Authenticated User** - Registered users with persistent favorites and profile management
- **Admin** - Full content and user management capabilities

### First-Time User Registration

After Firebase authentication, users must create their internal profile:

```bash
POST /api/user
Authorization: Bearer <firebase-id-token>
Content-Type: application/json

{
  "email": "user@example.com",
  "name": "John Doe"
}
```

The `ExternalAuthId` is automatically extracted from the JWT token.

### Admin Bootstrap

Administrators can be created via:
1. **Startup seeding** - Configure `AdminUser:SeedOnStartup=true` with credentials in environment variables
2. **Admin API** - Existing admins can create new admins via `POST /api/admin/user`

## Security Features

This API is production-ready with comprehensive security measures:

- **JWT Authentication** - Firebase-based token validation with role-based authorization
- **Rate Limiting** - Two policies:
  - **Fixed** (100 requests/minute) - Standard endpoints
  - **Strict** (10 requests/minute) - Sensitive operations (create, update, delete)
- **Security Headers** - CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy
- **CORS** - Configurable allowed origins for frontend integration
- **Soft Delete** - Data preservation with audit trail for users, tips, and categories
- **Audit Logging** - All administrative actions logged to Sentry with context
- **Input Validation** - Comprehensive validation with detailed error responses (RFC 7807)
- **Correlation IDs** - Request tracing across all logs and error responses

## Configuration and Environment Variables

Configuration is managed through `appsettings.json` files and environment variables. Environment-specific files (`appsettings.Development.json`, `appsettings.Production.json`) override base settings.

Any configuration value can be overridden with environment variables using the format `Section__Key` (double underscore).

### Database Configuration

- `UseInMemoryDB` - Set to `true` for in-memory database (testing), `false` for Firebase/Firestore (production)
- `Firebase__ProjectId` - Your Firebase project identifier
- `Firebase__DatabaseUrl` - Optional Firestore database URL
- `Firebase__EmulatorHost` - Optional Firestore emulator host:port for local testing

### Authentication Configuration (Firebase)

- `Authentication__Authority` - JWT issuer URL (e.g., `https://securetoken.google.com/<project-id>`)
- `Authentication__Audience` - JWT audience, typically your Firebase project ID

### CORS Configuration

- `ClientApp__Origin` - Allowed frontend origin (e.g., `http://localhost:3000` for development)
- Configure multiple origins in production as needed

### Admin Bootstrap Configuration

- `AdminUser__SeedOnStartup` - Set to `true` to create admin user on startup
- `AdminUser__Email` - Admin email address
- `AdminUser__DisplayName` - Admin display name
- `AdminUser__Password` - Admin password (use secrets management in production)

### Sentry Configuration (Optional)

- `Sentry__Enabled` - Toggle Sentry monitoring on/off
- `Sentry__EnableLogs` - Toggle Sentry log capture
- `Sentry__Dsn` - Your Sentry DSN (store in secrets, never commit)
- `Sentry__Environment` - Environment name (Development, Staging, Production)
- `Sentry__TracesSampleRate` - Performance monitoring sample rate (0.0 to 1.0)

### AWS S3 Configuration (for Image Upload)

- `AWS__S3__BucketName` - S3 bucket name for storing category images
- `AWS__S3__Region` - AWS region where the bucket is located (e.g., `us-east-1`)
- `AWS__CloudFront__Domain` - CloudFront distribution domain for CDN delivery

AWS credentials are managed through the AWS SDK's default credential chain:
- Environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`
- AWS credentials file: `~/.aws/credentials`
- IAM roles (recommended for production)

For detailed AWS setup instructions, see **[docs/AWS-S3-Setup-Guide.md](docs/AWS-S3-Setup-Guide.md)**

### Production Configuration

For production deployments:

1. **Set `AllowedHosts`** to your actual domain names
2. **Configure `ClientApp:Origin`** with your frontend URL(s)
3. **Use secrets management** for sensitive values (Firebase credentials, Sentry DSN, admin passwords)
4. **Set `UseInMemoryDB=false`** and provide Firebase configuration
5. **Enable Sentry** with appropriate sample rates
6. **Review rate limiting** policies in `RateLimitingConfiguration.cs`

### Example Environment Variables

```bash
# Database
export UseInMemoryDB=false
export Firebase__ProjectId=my-lifehacking-app
export Firebase__DatabaseUrl=https://my-lifehacking-app.firebaseio.com

# Authentication
export Authentication__Authority=https://securetoken.google.com/my-lifehacking-app
export Authentication__Audience=my-lifehacking-app

# CORS
export ClientApp__Origin=https://myapp.com

# Admin Bootstrap
export AdminUser__SeedOnStartup=true
export AdminUser__Email=admin@myapp.com
export AdminUser__DisplayName=Admin User
export AdminUser__Password=SecurePassword123!

# Sentry
export Sentry__Enabled=true
export Sentry__Dsn=https://your-sentry-dsn@sentry.io/project-id
export Sentry__Environment=Production
export Sentry__TracesSampleRate=0.1

# AWS S3 (for category image uploads)
export AWS_ACCESS_KEY_ID=your-access-key-id
export AWS_SECRET_ACCESS_KEY=your-secret-access-key
export AWS_REGION=us-east-1
export AWS__S3__BucketName=lifehacking-category-images-prod
export AWS__CloudFront__Domain=your-distribution.cloudfront.net
```

## Testing

The project includes comprehensive test coverage across all layers:

### Test Projects

- **Application.Tests** - Use case and domain logic tests
- **Infrastructure.Tests** - Repository and data access tests with Firestore emulator
- **WebAPI.Tests** - Integration tests for controllers and middleware

### Running Tests

```bash
# Run all tests
dotnet test lifehacking.slnx

# Run tests for a specific project
dotnet test lifehacking/Tests/Application.Tests/Application.Tests.csproj
dotnet test lifehacking/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj
dotnet test lifehacking/Tests/WebAPI.Tests/WebAPI.Tests.csproj

# Run a specific test
dotnet test --filter "Name=CreateTip_ShouldReturnValidationError_WhenTitleIsTooShort"
```

### Testing Approach

- **Microsoft Testing Platform** - Modern test runner with xUnit
- **FluentAssertions** - Expressive assertion syntax
- **Property-Based Testing** - Automated test case generation for domain invariants
- **Firestore Emulator** - Local Firestore instance for integration tests
- **Test Naming Convention** - `{MethodName}_Should{DoSomething}_When{Condition}`

For detailed testing guidelines, see `AGENTS.md`.

## Development Guidelines

### For AI Agents and Developers

- **AGENTS.md** - Comprehensive guidance for AI agents working with this codebase
- **ADRs/** - Architecture Decision Records documenting key technical choices
- **docs/MVP.md** - Product requirements and MVP scope

### Code Standards

- Clean Architecture with strict layer dependencies
- Domain-Driven Design principles
- No magic numbers or strings (use named constants)
- Comprehensive input validation
- Result pattern for error handling (no exceptions in normal flow)
- Soft-delete for data preservation

### Commit Conventions

- Follow [Conventional Commits](https://www.conventionalcommits.org/)
- Types: `feat`, `fix`, `chore`, `refactor`, `docs`, `test`
- Include issue references in commit footers

### Branching Strategy

- Feature branches: `issue-<ticket-id>-<short-description>`
- No direct commits to default branch
- Pull requests required for all changes

## API Response Formats

### Success Responses

All successful responses return JSON with appropriate HTTP status codes:

- `200 OK` - Successful GET, PUT requests
- `201 Created` - Successful POST requests (includes `Location` header)
- `204 No Content` - Successful DELETE requests

### Error Responses

All errors follow RFC 7807 Problem Details format:

```json
{
  "status": 400,
  "type": "https://httpstatuses.io/400/validation-error",
  "title": "Validation error",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/admin/tips",
  "correlationId": "abc123",
  "errors": {
    "Title": ["Tip title must be at least 5 characters"],
    "Description": ["Tip description cannot be empty"]
  }
}
```

Common error status codes:
- `400 Bad Request` - Validation errors, malformed requests
- `401 Unauthorized` - Missing or invalid authentication token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource does not exist
- `409 Conflict` - Duplicate resource (e.g., category name already exists)
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Unexpected server errors

All error responses include a `correlationId` for tracing requests across logs and monitoring systems.

## Contributing

Contributions are welcome! Please:

1. Review `AGENTS.md` for code standards and conventions
2. Check existing ADRs before making architectural changes
3. Follow the established testing patterns
4. Ensure all tests pass before submitting PRs
5. Update documentation for API changes

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Copyright (c) 2026 Lifehacking Tips API Contributors

---

**Built with Clean Architecture • Powered by Firebase & AWS • Deployed on Koyeb • AI-assisted with Kiro**
