# Lifehacking Tips API

A production-ready REST API designed for building practical tip discovery and management applications. Lifehacking provides a complete backend for exploring daily life tips, organizing them by categories, and managing user favorites with smooth anonymous-to-authenticated transitions.

Built with **.NET 10** and **Clean Architecture** principles. This project was created from [arielbvergara/clean-architecture](https://github.com/arielbvergara/clean-architecture) — a reusable Clean Architecture template developed during lessons and converted into a template. In that project you can see the commits made prior to the creation of this project.

> 🤖 AI-assisted development: [**Kiro**](https://kiro.dev) and [**Warp**](https://warp.dev) were used as AI assistants throughout the development of this project.


---

## 📋 Table of Contents

- [Deployed Application](#-deployed-application)
- [Project Presentation Slides](#project-presentation-slides)
- [Project Overview](#-project-overview)
- [Technology Stack](#️-technology-stack)
- [Related Projects](#-related-projects)
- [Key Features](#-key-features)
- [Installation & Running](#-installation--running)
- [Project Structure](#-project-structure)
- [API Endpoints](#-api-endpoints)
- [Architecture](#️-architecture)
- [Authentication & Authorization](#-authentication--authorization)
- [Security Features](#️-security-features)
- [Testing](#-testing)
- [Development Guidelines](#-development-guidelines)
- [Roadmap](#️-roadmap)

---

## 🌐 Deployed Application

- **Frontend:** [https://lifehacking.vercel.app/](https://lifehacking.vercel.app/)
- **Backend API:** [https://slight-janet-lifehacking-ce47cbe0.koyeb.app/](https://slight-janet-lifehacking-ce47cbe0.koyeb.app/)


---

## Project Presentation Slides

[Lifehacking Master Slides Presentation](lifehacking-master-presentation.pptx)

---

## 🎯 Project Overview


### What is the Lifehacking Tips API?

The Lifehacking Tips API is a complete and robust backend solution that enables building applications where users can discover, organize, and manage practical tips to improve their daily lives. The API is designed with modern architecture and industry best practices, providing a solid foundation for web and mobile applications.

### Main Use Cases

The API allows developers to build applications where users can:

- **Discover tips** through advanced search, filtering by categories and tags (no authentication required)
- **Save favorites** with automatic sync between local storage and server-side persistence
- **Manage content** through a complete administrative interface for tips and categories
- **Manage users** with Firebase authentication, role-based access control, and self-service account management

### Supported User Types

The system is designed to support three user types with different access levels:

1. **Anonymous Users** — Full read access with client-side favorites
2. **Authenticated Users** — Persistent favorites with automatic merge from local storage
3. **Administrators** — Full content and user management capabilities

### Design Philosophy

The project follows the principles of **Clean Architecture** and **Domain-Driven Design (DDD)**, ensuring:

- Clear separation of concerns between layers
- Business domain independence from frameworks and external technologies
- Maintainable, testable, and scalable code
- Easy addition of new features without affecting existing code

---

## 🛠️ Technology Stack

### Backend (this repository)


| Technology | Purpose |
|-----------|-----------|
| **.NET 10 + Clean Architecture** | Web API with Domain, Application, Infrastructure, and WebAPI layers |
| **Firebase Authentication** | JWT Bearer token validation and identity management |
| **Firebase Cloud Firestore** | Primary NoSQL database |
| **AWS S3** | Category image storage |
| **AWS CloudFront** | CDN for image delivery |
| **Docker & Docker Compose** | Containerized deployment |
| **[Koyeb](https://app.koyeb.com)** | Cloud deployment platform |
| **Dependabot** | Automatic weekly dependency updates |
| **GitHub Actions** | CI pipeline (build, test, lint, security scanning) and code review |
| **[Kiro](https://kiro.dev)** | AI assistant used during development |
| **[Warp](https://warp.dev)** | AI assistant used during development |
| **[Sentry](https://sentry.io)** | Error tracking and performance monitoring |
| **Swagger / OpenAPI** | Interactive API documentation |
| **Github Copilot** | AI assistant used for code review |

### Frontend ([lifehacking-app](https://github.com/arielbvergara/lifehacking-app))

| Technology | Purpose |
|-----------|---------|
| **Next.js 16** | React-based frontend framework |
| **Google Stitch** | UI/UX design |
| **Firebase Authentication** | Authentication and identity |
| **Vercel** | Frontend deployment |
| **Sentry.io** | Monitoring and error tracking |
| **Docker** | Containerized deployment |
| **Dependabot** | Automatic weekly dependency updates |
| **GitHub Actions** | CI pipeline and code review |
| **Kiro** | AI assistant used during development |
| **Github Copilot** | AI assistant used for code review |

---

## 🔗 Related Projects

| Project | Description | Deployment |
|---------|-------------|------------|
| **[lifehacking-app](https://github.com/arielbvergara/lifehacking-app)** | Frontend — Next.js 16, Google Stitch design, Firebase, Docker, Vercel | [Vercel](https://vercel.com) |
| **lifehacking** *(this repository)* | Backend API — .NET 10, Clean Architecture, Firebase, Docker, AWS | [Koyeb](https://app.koyeb.com) |

---

## ✨ Key Features


### For Anonymous Users (Public API)

- **Tip exploration** with advanced search and filtering (by category, tags, search term)
- **Detailed view** of tip information including step-by-step instructions
- **Category browsing** with access to all available categories
- **Flexible sorting** of results by creation date, update date, or title
- **Paginated responses** for optimal performance
- **Client-side favorites management** (local storage)

### For Authenticated Users

- **All anonymous user capabilities**
- **Persistent favorites** stored server-side
- **Automatic merge** of local favorites on first login (no duplicates)
- **Cross-device sync** of favorites
- **Self-service profile management** (view, update name, delete account)

### For Administrators

- **All authenticated user capabilities**
- **Full tip lifecycle management** (create, update, delete)
- **Category management** with cascade deletion
- **Complete user administration**
- **Admin user creation** with Firebase integration
- **Dashboard** with real-time statistics and entity counts
- **Audit log** for all administrative actions

### Notable Technical Features

- **Clean Architecture** with clear separation of concerns
- **In-memory cache** with automatic invalidation for performance optimization
- **Soft Delete** for data preservation and auditing
- **Exhaustive input validation** with detailed error responses
- **Correlation IDs** for request traceability in logs and monitoring systems
- **Interactive documentation** with Swagger/OpenAPI
- **Robust security** with JWT, rate limiting, security headers, and configurable CORS

---

## 🚀 Installation & Running

### 1. Prerequisites

Before getting started, make sure you have installed:

- [.NET SDK 10.0](https://dotnet.microsoft.com/) or higher
- [Docker](https://www.docker.com/) and Docker Compose
- A Firebase project for authentication (create one at [Firebase Console](https://console.firebase.google.com/))
- Optional: A Sentry project for monitoring (sign up at [sentry.io](https://sentry.io/))


### 2. Quick Start with Docker Compose

The fastest way to run the API locally with all dependencies configured.

#### Prerequisites for Docker Compose

Before running `docker compose up`, you need:

1. **Firebase Admin SDK credentials file**
   - Download the JSON credentials file from [Firebase Console](https://console.firebase.google.com/)
   - Go to: Project Settings → Service Accounts → Generate new private key
   - Save the file as `firebase-adminsdk.json` in `~/secrets/`

   ```bash
   # Create directory if it doesn't exist
   mkdir -p ~/secrets

   # Copy your credentials file
   cp /path/to/your/firebase-adminsdk.json ~/secrets/firebase-adminsdk.json
   ```

2. **Configure environment variables in docker-compose.yml** (already configured by default):
   - `ASPNETCORE_ENVIRONMENT: Development` — Development environment
   - `ClientApp__Origin: "http://localhost:3000"` — Frontend origin for CORS
   - `GOOGLE_APPLICATION_CREDENTIALS: /app/firebase-adminsdk.json` — Path to credentials inside the container

3. **Configure Firebase in appsettings.json or environment variables**

   Option A: Edit `lifehacking/WebAPI/appsettings.Development.json`:
   ```json
   {
     "Firebase": {
       "ProjectId": "your-firebase-project",
     },
     "Authentication": {
       "Authority": "https://securetoken.google.com/your-firebase-project",
       "Audience": "your-firebase-project"
     }
   }
   ```

   Option B: Add environment variables in `docker-compose.yml`:
   ```yaml
   environment:
     Firebase__ProjectId: "your-firebase-project"
     Authentication__Authority: "https://securetoken.google.com/your-firebase-project"
     Authentication__Audience: "your-firebase-project"
   ```

#### Run with Docker Compose

Once configured:

```bash
docker compose up --build
```

This will:
- Build the Docker image with .NET 10
- Mount the Firebase credentials file
- Start the WebAPI container
- Configure the API to use Firebase/Firestore
- Expose the API on port 8080

Once running:
- **API Base URL**: `http://localhost:8080`
- **Swagger UI**: `http://localhost:8080/swagger` (interactive API documentation)
- **Health Check**: `http://localhost:8080/health` (if configured)

To stop the services:

```bash
docker compose down
```

**Important note:** The project is designed to use Firebase/Firestore as the database. Docker Compose is configured to automatically connect to your Firebase project.

### 3. Run the WebAPI Directly (Without Docker)

For faster iteration during development, run the API directly using the .NET SDK:

```bash
# Build the solution
dotnet build lifehacking.slnx

# Run the WebAPI project
dotnet run --project lifehacking/WebAPI/WebAPI.csproj
```

The API reads configuration from `lifehacking/WebAPI/appsettings.Development.json` and environment variables, connecting to Firebase/Firestore as configured.

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

2. **Get a Firebase ID token**:
   - Authenticate a user through Firebase (web, mobile, or REST API)
   - Extract the ID token from the authentication response

3. **Use the token in API requests**:
   ```bash
   curl -H "Authorization: Bearer <firebase-id-token>" \
        http://localhost:8080/api/user/me
   ```

The API validates the JWT token and maps the `sub` claim to the internal user's `ExternalAuthId`.


### 5. Configure Monitoring with Sentry (Optional)

Sentry integration is optional. The API works normally with Sentry disabled.

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

When enabled, unhandled errors and performance traces are sent to Sentry with full context (path, user, correlation ID).

### 6. Explore the API with Swagger

Once the API is running, navigate to the Swagger UI for interactive documentation:

**http://localhost:8080/swagger**

Swagger provides:
- Complete endpoint documentation with request/response schemas
- Validation rules and constraints
- Interactive testing (try endpoints directly from the browser)
- Authentication support (add your Bearer token to test protected endpoints)

Note: Swagger is only enabled in non-production environments.

### 7. Configure AWS S3 for Image Uploads

To enable category image uploads, configure AWS S3 and CloudFront:

```bash
export AWS_ACCESS_KEY_ID=your-access-key-id
export AWS_SECRET_ACCESS_KEY=your-secret-access-key
export AWS_REGION=us-east-1
export AWS__S3__BucketName=lifehacking-category-images
export AWS__CloudFront__Domain=your-distribution.cloudfront.net
```

For detailed AWS setup instructions, see **[docs/AWS-S3-Setup-Guide.md](docs/AWS-S3-Setup-Guide.md)**

---

## 📁 Project Structure


The project follows **Clean Architecture** principles with a clear separation of concerns:

```
lifehacking/
├── lifehacking.slnx                 # .NET 10 solution file
├── README.md                        # Main documentation (English)
├── AGENTS.md                        # Guide for AI agents
├── docker-compose.yml               # Docker Compose configuration
├── Dockerfile                       # Application Docker image
│
├── ADRs/                            # Architecture Decision Records
│   ├── 001-use-microsoft-testing-platform-runner.md
│   ├── 018-replace-postgresql-persistence-with-firebase-database.md
│   ├── 020-user-favorites-domain-model-and-storage.md
│   └── ...                          # More architectural decisions
│
├── docs/                            # Additional documentation
│   ├── MVP.md                       # Product requirements and MVP scope
│   ├── AWS-S3-Setup-Guide.md        # AWS S3 configuration guide
│   └── Search-Architecture-Decision.md
│
└── lifehacking/                     # Main source code
    │
    ├── Domain/                      # Domain Layer
    │   ├── Entities/                # Domain entities (User, Tip, Category, UserFavorites)
    │   ├── ValueObject/             # Value objects (CategoryImage, etc.)
    │   ├── Primitives/              # Primitive types (Result<T, TE>)
    │   └── Constants/               # Domain constants (ImageConstants)
    │
    ├── Application/                 # Application Layer
    │   ├── UseCases/                # Use cases organized by feature
    │   │   ├── User/                # User use cases
    │   │   ├── Category/            # Category use cases
    │   │   ├── Tip/                 # Tip use cases
    │   │   ├── Favorite/            # Favorites use cases
    │   │   └── Dashboard/           # Dashboard use cases
    │   ├── Dtos/                    # Data Transfer Objects
    │   │   ├── User/                # User DTOs
    │   │   ├── Category/            # Category DTOs
    │   │   ├── Tip/                 # Tip DTOs
    │   │   ├── Favorite/            # Favorites DTOs
    │   │   └── Dashboard/           # Dashboard DTOs
    │   ├── Interfaces/              # Interfaces (ports)
    │   │   ├── IUserRepository
    │   │   ├── ICategoryRepository
    │   │   ├── IImageStorageService
    │   │   └── ICacheInvalidationService
    │   ├── Exceptions/              # Application exceptions
    │   ├── Validation/              # Validation utilities
    │   └── Caching/                 # Cache key definitions
    │
    ├── Infrastructure/              # Infrastructure Layer
    │   ├── Data/Firestore/          # Firestore implementation
    │   │   ├── Documents/           # Firestore document classes
    │   │   └── DataStores/          # Data stores (entity-document mapping)
    │   ├── Repositories/            # Repository implementations
    │   ├── Storage/                 # Cloud storage services
    │   │   └── S3ImageStorageService.cs
    │   └── Configuration/           # Configuration option classes
    │
    ├── WebAPI/                      # Web API Layer
    │   ├── Program.cs               # Entry point and root composition
    │   ├── Controllers/             # REST controllers
    │   │   ├── UserController.cs
    │   │   ├── AdminCategoryController.cs
    │   │   ├── AdminDashboardController.cs
    │   │   └── ...
    │   ├── Filters/                 # Global filters
    │   │   └── GlobalExceptionFilter.cs
    │   ├── Configuration/           # Service configuration
    │   ├── appsettings.json         # Base configuration
    │   ├── appsettings.Development.json
    │   └── appsettings.Production.json
    │
    └── Tests/                       # Test projects
        ├── Application.Tests/       # Application layer tests
        ├── Infrastructure.Tests/    # Infrastructure layer tests
        └── WebAPI.Tests/            # API integration tests

```

### Dependency Direction

The project strictly follows Clean Architecture dependency rules:

- **Domain** → No references to other projects (completely independent)
- **Application** → Depends only on **Domain**
- **Infrastructure** → Depends on **Application** and **Domain**
- **WebAPI** → Depends on **Application**, **Domain**, and **Infrastructure**
- **Tests** → Reference only the layers they are intended to validate

### HTTP Request Flow

```
HTTP Client
    ↓
WebAPI Controller (presentation layer)
    ↓
Application Use Case (business logic)
    ↓
Domain Entities/Value Objects (domain model)
    ↓
Infrastructure Repository (data access)
    ↓
Firestore/Firebase (persistence)
    ↓
Result<T, AppException> (response)
    ↓
HTTP Response (mapped to status codes)
```

---

## 🔌 API Endpoints


All endpoints return JSON and follow RFC 7807 Problem Details for error responses. Each response includes a `correlationId` for request traceability.

For complete request/response schemas, validation rules, and interactive testing, see the **Swagger UI** at `http://localhost:8080/swagger` when running the API.

### Public Endpoints (No Authentication Required)

#### Tips API - `/api/tip`

- **`GET /api/tip`** — Search and filter tips
  - Query parameters: `q` (search term), `categoryId`, `tags[]`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Returns paginated tip summaries with metadata

- **`GET /api/tip/{id}`** — Get full tip details
  - Returns complete tip with title, description, ordered steps, category, tags, and optional video URL

#### Categories API - `/api/category`

- **`GET /api/category`** — List all available categories
  - Returns all non-deleted categories

- **`GET /api/category/{id}/tips`** — Get tips by category
  - Query parameters: `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Returns paginated tips for the specified category

### Authenticated Endpoints (Requires JWT Bearer Token)

#### User API - `/api/user`

- **`POST /api/user`** — Create user profile after authentication
  - Called once after Firebase authentication to create the internal user record
  - External auth ID derived from the JWT token

- **`GET /api/user/me`** — Get current user profile
  - User resolved from the JWT token

- **`PUT /api/user/me/name`** — Update current user's display name
  - Self-service profile update

- **`DELETE /api/user/me`** — Delete current user's account
  - Soft delete with audit log

#### Favorites API - `/api/me/favorites`

- **`GET /api/me/favorites`** — List user's favorite tips
  - Query parameters: `q`, `categoryId`, `tags[]`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Returns paginated favorites with full tip details

- **`POST /api/me/favorites/{tipId}`** — Add tip to favorites
  - Idempotent operation

- **`DELETE /api/me/favorites/{tipId}`** — Remove tip from favorites

- **`POST /api/me/favorites/merge`** — Merge local favorites from client storage
  - Accepts array of tip IDs from local storage
  - Returns summary with added, skipped, and failed counts
  - Idempotent and supports partial success


### Admin Endpoints (Requires Admin Role)

#### Admin Tips API - `/api/admin/tips`

- **`POST /api/admin/tips`** — Create new tip
  - Required: title, description, steps (ordered list), categoryId
  - Optional: tags (max 10), videoUrl (YouTube/Instagram)

- **`PUT /api/admin/tips/{id}`** — Update existing tip
  - All fields updatable

- **`DELETE /api/admin/tips/{id}`** — Soft delete tip
  - Marks the tip as deleted, preserves data

#### Admin Categories API - `/api/admin/categories`

- **`POST /api/admin/categories/images`** — Upload category image
  - Accepts multipart/form-data with image file
  - Validates file size (max 5MB), content type (JPEG, PNG, GIF, WebP), and magic bytes
  - Uploads to AWS S3 with GUID-based unique filename
  - Returns image metadata including CloudFront CDN URL
  - Required before creating categories with images

- **`POST /api/admin/categories`** — Create new category
  - Required: name (2-100 characters, case-insensitive unique)
  - Optional: image metadata from upload endpoint

- **`PUT /api/admin/categories/{id}`** — Update category name
  - Uniqueness enforced

- **`DELETE /api/admin/categories/{id}`** — Soft delete category
  - Cascade soft delete to all associated tips

#### Admin Users API - `/api/admin/user`

- **`POST /api/admin/user`** — Create admin user
  - Creates user in Firebase and internal database
  - Required: email, displayName, password

- **`GET /api/admin/user`** — List users with pagination
  - Query parameters: `search`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`, `isDeleted`
  - Supports search by email, name, and ID

- **`GET /api/admin/user/{id}`** — Get user by internal ID

- **`GET /api/admin/user/email/{email}`** — Get user by email address

- **`PUT /api/admin/user/{id}/name`** — Update user's display name

- **`DELETE /api/admin/user/{id}`** — Soft delete user account

#### Admin Dashboard API - `/api/admin/dashboard`

- **`GET /api/admin/dashboard`** — Get dashboard statistics
  - Returns entity counts for users, categories, and tips
  - Results cached for 1 hour for optimal performance
  - Provides a quick overview for administrative monitoring

---

## 🏗️ Architecture


This API follows **Clean Architecture** principles with clear separation of concerns:

### Architecture Layers

#### 1. Domain Layer

**Responsibility:** Contains the core business logic and domain rules.

**Features:**
- Business entities (User, Tip, Category, UserFavorites)
- Value objects (CategoryImage)
- Domain primitive types (Result<T, TE>)
- Domain constants (ImageConstants)
- No external dependencies (completely independent)
- Persistence and framework agnostic

**Principle:** The domain is the heart of the application and must not depend on anything external.

#### 2. Application Layer

**Responsibility:** Orchestrates use cases and coordinates data flow.

**Features:**
- Use cases organized by feature (User, Category, Tip, Favorite, Dashboard)
- DTOs (Data Transfer Objects) for communication with the presentation layer
- Interfaces (ports) for external services (IUserRepository, ICategoryRepository, IImageStorageService)
- Validation and transformation logic
- Cache management with automatic invalidation
- Application exception handling

**Principle:** Defines what the system does without caring about how it does it.

#### 3. Infrastructure Layer

**Responsibility:** Implements technical details and external services.

**Features:**
- Repository implementations (UserRepository, CategoryRepository)
- Data access with Firestore (documents, data stores)
- Cloud storage services (S3ImageStorageService)
- Firebase Authentication integration
- External service configuration (AWS, Firebase)
- Mapping between domain entities and persistence documents

**Principle:** Provides concrete implementations of the abstractions defined in Application.

#### 4. Web API Layer

**Responsibility:** Exposes functionality through HTTP REST endpoints.

**Features:**
- REST controllers organized by feature
- Authentication and authorization middleware
- Global filters (GlobalExceptionFilter)
- Service configuration and root composition (Program.cs)
- Swagger/OpenAPI documentation
- Mapping of Result<T, AppException> to HTTP status codes

**Principle:** Thin layer focused on HTTP concerns, delegating logic to Application.

### Architectural Patterns Applied

#### Result Pattern

Instead of throwing exceptions for normal control flow, the Result pattern is used:

```csharp
Result<TipDetailResponse, AppException> result = await useCase.ExecuteAsync(request);

return result.Match(
    success => Ok(success),
    error => error.ToActionResult()
);
```

**Benefits:**
- Explicit error handling
- Better performance (no stack unwinding)
- More predictable and testable code

#### Dependency Injection

All dependencies are injected through constructors:

```csharp
public class CreateTipUseCase
{
    private readonly ITipRepository _tipRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateTipUseCase(
        ITipRepository tipRepository,
        ICategoryRepository categoryRepository)
    {
        _tipRepository = tipRepository;
        _categoryRepository = categoryRepository;
    }
}
```

**Benefits:**
- Facilitates testing with mocks
- Low coupling
- Easy substitution of implementations

#### Repository Pattern

Abstracts data access behind interfaces:

```csharp
public interface ITipRepository
{
    Task<Tip?> GetByIdAsync(Guid id);
    Task<PagedResult<Tip>> SearchAsync(TipQueryCriteria criteria);
    Task<Tip> CreateAsync(Tip tip);
    Task UpdateAsync(Tip tip);
    Task DeleteAsync(Guid id);
}
```

**Benefits:**
- Independence from persistence technology
- Facilitates database changes
- Improves testability

### Documented Architectural Decisions

Key architectural decisions are documented in `ADRs/`:

- **ADR-018** — Replacement of PostgreSQL with Firebase Firestore
- **ADR-020** — User favorites domain model and storage
- **ADR-006** — User roles and soft delete lifecycle
- **ADR-010** — Hardened production configuration
- **ADR-011** — Security headers and rate limiting
- **ADR-013** — Standardized error handling and security logging
- **ADR-015** — Sentry monitoring and observability integration

---

## 🔐 Authentication & Authorization


### Authentication Flow

The system uses Firebase Authentication with JWT Bearer tokens:

1. **User authenticates with Firebase** (your frontend handles this)
2. **Frontend receives Firebase ID token** (JWT)
3. **Frontend calls the API with the token** in the `Authorization: Bearer <token>` header
4. **API validates the token** with Firebase and extracts the user's identity
5. **API maps the Firebase UID** to the internal user record

### User Types and Permissions

#### Anonymous Users
- **Access:** No authentication required
- **Permissions:**
  - Full read access to tips and categories
  - Advanced search and filtering
  - Client-side favorites management (local storage)

#### Authenticated Users
- **Access:** Requires valid JWT token
- **Permissions:**
  - All anonymous user permissions
  - Server-side persistent favorites
  - Profile management (view, update name)
  - Account deletion (self-service)
  - Local favorites merge

#### Administrators
- **Access:** Requires valid JWT token with Admin role
- **Permissions:**
  - All authenticated user permissions
  - Full tip management (create, update, delete)
  - Category management (create, update, delete, upload images)
  - User administration (create, list, update, delete)
  - Dashboard access with statistics

### First-Time User Registration

After authenticating with Firebase, users must create their internal profile:

```bash
POST /api/user
Authorization: Bearer <firebase-id-token>
Content-Type: application/json

{
  "email": "user@example.com",
  "name": "John Doe"
}
```

The `ExternalAuthId` is automatically extracted from the JWT token (`sub` claim).

### Admin Bootstrap

Administrators can be created via:

1. **Seeding on startup** — Set `AdminUser:SeedOnStartup=true` with credentials in environment variables
2. **Admin API** — Existing administrators can create new admins via `POST /api/admin/user`

**Example seeding configuration:**

```json
{
  "AdminUser": {
    "SeedOnStartup": true,
    "Email": "admin@example.com",
    "DisplayName": "Administrator",
    "Password": "SecurePassword123!"
  }
}
```

### JWT Token Validation

The API automatically validates JWT tokens using the Firebase configuration:

```json
{
  "Authentication": {
    "Authority": "https://securetoken.google.com/<your-project-id>",
    "Audience": "<your-project-id>"
  }
}
```

**Important JWT claims:**
- `sub` — Firebase UID (mapped to ExternalAuthId)
- `email` — User email
- `email_verified` — Email verification status
- `role` — Custom role (User or Admin)

---

## 🛡️ Security Features


This API is production-ready with exhaustive security measures:

### Authentication and Authorization

- **JWT Authentication** — Firebase-based token validation with role-based authorization
- **Role-Based Access Control (RBAC)** — Clear separation between anonymous, authenticated, and admin users
- **Token Validation** — Automatic validation of JWT token signature, expiration, and audience
- **Secure Claims Mapping** — Secure mapping of JWT claims to internal user identity

### Rate Limiting

Two rate limiting policies to protect against abuse:

#### Fixed Policy
- **Limit:** 100 requests per minute
- **Applied to:** Standard read and write endpoints
- **Window:** 1-minute sliding window

#### Strict Policy
- **Limit:** 10 requests per minute
- **Applied to:** Sensitive operations (create, update, delete)
- **Window:** 1-minute sliding window

**Response when limit is exceeded:**
```json
{
  "status": 429,
  "type": "https://httpstatuses.io/429",
  "title": "Too Many Requests",
  "detail": "Rate limit exceeded. Please try again later.",
  "instance": "/api/admin/tips",
  "correlationId": "abc123"
}
```

### Security Headers

The API automatically configures HTTP security headers:

- **Content-Security-Policy (CSP)** — Prevents XSS attacks
- **Strict-Transport-Security (HSTS)** — Forces HTTPS connections
- **X-Frame-Options** — Prevents clickjacking
- **X-Content-Type-Options** — Prevents MIME sniffing
- **Referrer-Policy** — Controls referrer information
- **Permissions-Policy** — Controls browser features

### CORS (Cross-Origin Resource Sharing)

Flexible CORS configuration for frontend integration:

```json
{
  "ClientApp": {
    "Origin": "https://your-app.com"
  }
}
```

**Features:**
- Configurable origins per environment
- Multiple origins supported in production
- Specific allowed headers
- Controlled allowed HTTP methods

### Input Validation

Exhaustive validation at multiple levels:

#### DTO Validation
- Data annotations on DTOs
- Automatic validation in the ASP.NET Core pipeline
- Descriptive error messages

#### Domain Validation
- Business rules in entities
- Value objects with built-in validation
- Domain invariant validation

#### File Validation
- **Magic Byte Validation** — Prevents content type spoofing
- **Filename Sanitization** — Prevents path traversal vulnerabilities
- **Size Validation** — Limits defined in constants (max 5MB for images)
- **MIME Type Validation** — Only allowed types (JPEG, PNG, GIF, WebP)

### Soft Delete

Data preservation with audit log:

- **Users** — Marked as deleted, data preserved
- **Tips** — Marked as deleted, relationships preserved
- **Categories** — Cascade soft delete to related tips
- **Audit** — Deletion timestamps for traceability

### Logging and Auditing

Complete logging system with Sentry integration:

- **Correlation IDs** — Request traceability across all logs
- **Structured Logging** — Structured logs with rich context
- **Security Events** — Logging of security events (authentication, authorization)
- **Error Tracking** — Automatic capture of unhandled exceptions
- **Performance Monitoring** — Performance traces with configurable sample rate

### Standardized Error Handling

Consistent error responses following RFC 7807:

```csharp
{
  "status": 400,
  "type": "https://httpstatuses.io/400/validation-error",
  "title": "Validation error",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/admin/tips",
  "correlationId": "abc123",
  "errors": {
    "Title": ["The tip title must be at least 5 characters long"]
  }
}
```

**Benefits:**
- Industry-standard format
- Detailed error information without exposing implementation details
- Correlation IDs for support and debugging
- Consistent responses across the entire API

### Protection Against Common Vulnerabilities

- **SQL Injection** — Not applicable (NoSQL with Firestore)
- **XSS (Cross-Site Scripting)** — CSP headers and input sanitization
- **CSRF (Cross-Site Request Forgery)** — Stateless JWT tokens
- **Path Traversal** — Filename sanitization
- **Content Type Spoofing** — Magic byte validation
- **Denial of Service** — Rate limiting and configurable timeouts
- **Information Disclosure** — Generic error messages in production

---

## 🧪 Testing


The project includes exhaustive test coverage across all layers:

### Test Projects

- **Application.Tests** — Use case and domain logic tests
- **Infrastructure.Tests** — Repository and data access tests with Firestore emulator
- **WebAPI.Tests** — Integration tests for controllers and middleware

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

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~CreateTipUseCaseTests"
```

### Testing Approach

#### Microsoft Testing Platform

The project uses **Microsoft Testing Platform** as the modern test runner:

```xml
<PropertyGroup>
  <DotNetTestRunner>Microsoft.Testing.Platform</DotNetTestRunner>
</PropertyGroup>
```

**Benefits:**
- Improved performance
- Better integration with development tools
- Support for property-based testing

#### xUnit Framework

All tests use xUnit as the testing framework:

```csharp
[Fact]
public async Task CreateTip_ShouldReturnSuccess_WhenDataIsValid()
{
    // Arrange
    var request = new CreateTipRequest { /* ... */ };

    // Act
    var result = await _useCase.ExecuteAsync(request);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

#### FluentAssertions

Expressive and readable assertion syntax:

```csharp
// Instead of
Assert.Equal(expected, actual);
Assert.True(condition);

// We use
actual.Should().Be(expected);
condition.Should().BeTrue();
result.Should().NotBeNull();
list.Should().HaveCount(5);
```

**Benefits:**
- More descriptive error messages
- More natural and readable syntax
- Better developer experience


#### Firestore Emulator

Infrastructure tests use the local Firestore emulator:

```bash
# Start emulator
firebase emulators:start --only firestore

# Tests connect automatically to the emulator
export FIRESTORE_EMULATOR_HOST=localhost:8080
```

**Benefits:**
- Realistic integration tests
- No Firebase costs
- Isolated data per test run
- Fast execution speed

### Test Naming Convention

All tests follow the pattern:

```
{MethodName}_Should{DoSomething}_When{Condition}
```

**Examples:**
```csharp
CreateTip_ShouldReturnSuccess_WhenDataIsValid()
CreateTip_ShouldReturnValidationError_WhenTitleIsTooShort()
GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
AddFavorite_ShouldBeIdempotent_WhenCalledMultipleTimes()
```

**Benefits:**
- Self-descriptive names
- Easy identification of scenarios
- Living documentation of behavior

### Test Organization

Tests are organized by feature and layer:

```
Tests/
├── Application.Tests/
│   ├── UseCases/
│   │   ├── User/
│   │   │   ├── CreateUserUseCaseTests.cs
│   │   │   ├── DeleteUserUseCaseTests.cs
│   │   │   └── UpdateUserNameUseCaseTests.cs
│   │   ├── Category/
│   │   │   ├── CreateCategoryUseCaseTests.cs
│   │   │   └── DeleteCategoryUseCaseTests.cs
│   │   ├── Tip/
│   │   │   ├── CreateTipUseCaseTests.cs
│   │   │   └── SearchTipsUseCaseTests.cs
│   │   ├── Favorite/
│   │   │   ├── AddFavoriteUseCaseTests.cs
│   │   │   └── MergeFavoritesUseCaseTests.cs
│   │   └── Dashboard/
│   │       └── GetDashboardUseCaseTests.cs
│   └── MicrosoftTestingPlatformSmokeTests.cs
│
├── Infrastructure.Tests/
│   ├── Repositories/
│   │   ├── UserRepositoryTests.cs
│   │   ├── CategoryRepositoryTests.cs
│   │   └── TipRepositoryTests.cs
│   └── Storage/
│       └── S3ImageStorageServiceTests.cs
│
└── WebAPI.Tests/
    ├── Controllers/
    │   ├── UserControllerTests.cs
    │   ├── AdminCategoryControllerTests.cs
    │   └── AdminDashboardControllerTests.cs
    └── Filters/
        └── GlobalExceptionFilterTests.cs
```

### Testing Strategies

#### Unit Tests (Application.Tests)

- Test isolated use cases
- Use mocks for dependencies (repositories, services)
- Verify business logic and validation
- Test cache behavior

```csharp
[Fact]
public async Task GetDashboard_ShouldReturnCachedData_WhenCacheHit()
{
    // Arrange
    var cachedData = new DashboardResponse { /* ... */ };
    _cache.Set(CacheKeys.Dashboard, cachedData);

    // Act
    var result = await _useCase.ExecuteAsync(new GetDashboardRequest());

    // Assert
    result.IsSuccess.Should().BeTrue();
    _mockRepository.Verify(r => r.GetStatistics(), Times.Never);
}
```

#### Integration Tests (Infrastructure.Tests)

- Test repositories with Firestore emulator
- Verify mapping between entities and documents
- Test complex queries and filters
- Validate persistence behavior

```csharp
[Fact]
public async Task CreateUser_ShouldPersistToFirestore_WhenDataIsValid()
{
    // Arrange
    var user = User.Create(/* ... */);

    // Act
    await _repository.CreateAsync(user);

    // Assert
    var retrieved = await _repository.GetByIdAsync(user.Id);
    retrieved.Should().NotBeNull();
    retrieved.Email.Should().Be(user.Email);
}
```

#### API Tests (WebAPI.Tests)

- Test complete HTTP endpoints
- Verify status codes and responses
- Validate authentication and authorization
- Test error handling

```csharp
[Fact]
public async Task CreateTip_ShouldReturn401_WhenNotAuthenticated()
{
    // Arrange
    var request = new CreateTipRequest { /* ... */ };

    // Act
    var response = await _client.PostAsJsonAsync("/api/admin/tips", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

### Test Coverage

The project maintains high test coverage:

- **Use cases:** >90% coverage
- **Repositories:** >85% coverage
- **Controllers:** >80% coverage
- **Domain logic:** 100% coverage

To generate a coverage report:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 📚 Development Guidelines


### For AI Agents and Developers

The project includes exhaustive documentation to facilitate development:

- **AGENTS.md** — Complete guide for AI agents working with this code
- **ADRs/** — Architecture Decision Records documenting key technical decisions
- **docs/MVP.md** — Product requirements and MVP scope
- **docs/AWS-S3-Setup-Guide.md** — Detailed AWS S3 configuration guide

### Code Standards

#### Clean Architecture

- Maintain strict dependencies between layers
- Domain must not depend on anything
- Application only depends on Domain
- Infrastructure implements Application interfaces
- WebAPI is the composition layer

#### Domain-Driven Design (DDD)

- Use rich entities with behavior
- Encapsulate business logic in the domain
- Use value objects for concepts without identity
- Keep aggregates consistent

#### No Magic Values

```csharp
// ❌ Wrong
if (file.Length > 5242880) { /* ... */ }

// ✅ Correct
if (file.Length > ImageConstants.MaxFileSizeInBytes) { /* ... */ }
```

**Rules:**
- Define constants with meaningful names
- Centralize reusable values
- Use enums for related value sets
- Self-descriptive names that express intent

#### Exhaustive Validation

```csharp
public class CreateTipRequest
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Title { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; }

    [Required]
    [MinLength(1)]
    public List<TipStepRequest> Steps { get; set; }
}
```

#### Result Pattern for Error Handling

```csharp
// Instead of throwing exceptions
public async Task<Result<TipDetailResponse, AppException>> ExecuteAsync(
    CreateTipRequest request)
{
    if (!await _categoryRepository.ExistsAsync(request.CategoryId))
    {
        return new NotFoundException("Category not found");
    }

    var tip = Tip.Create(/* ... */);
    await _repository.CreateAsync(tip);

    return tip.ToDetailResponse();
}
```

#### Soft Delete for Data Preservation

```csharp
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete()
    {
        DeletedAt = DateTime.UtcNow;
    }

    public bool IsDeleted => DeletedAt.HasValue;
}
```

### Commit Conventions

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>: <description>

<optional body>

<optional footer>
```

**Allowed types:**
- `feat` — New feature
- `fix` — Bug fix
- `chore` — Maintenance tasks
- `refactor` — Code refactoring
- `docs` — Documentation changes
- `test` — Adding or modifying tests

**Examples:**
```
feat: add user favorites merge endpoint

Implements automatic merge of local favorites when user logs in
for the first time. Handles deduplication and partial failures.

refs: WT-1234
```

```
fix: correct cache invalidation on category delete

Categories were not being removed from cache when deleted,
causing stale data to be served.

refs: WT-5678
```

### Branching Strategy

- **Feature branches:** `issue-<ticket-id>-<short-description>`
- **No direct commits** to the main branch
- **Pull requests required** for all changes
- **Code review** before merging

**Example:**
```bash
# Create branch from issue
git checkout -b issue-123-add-favorites-merge

# Make commits
git commit -m "feat: add merge favorites use case"
git commit -m "test: add merge favorites tests"

# Push and create PR
git push origin issue-123-add-favorites-merge
```

---

## 🗺️ Roadmap

Planned features for future versions:

### v2.0
- [ ] Full-text search with Algolia or Elasticsearch
- [ ] Comments and tip rating system
- [ ] Push notifications for new tips

### v2.1
- [ ] Multi-language support for tips
- [ ] AI-based personalized recommendations
- [ ] Social media sharing integration
- [ ] Advanced statistics for administrators

### v3.0
- [ ] Native mobile application (iOS and Android)
- [ ] Offline mode with synchronization
- [ ] Gamification (badges, achievements, levels)
- [ ] User community with public profiles
