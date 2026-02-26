# API de Consejos Lifehacking

> **Nota:** Se generarán credenciales de administrador y se enviarán de forma segura como parte de la entrega al mail: mouredev@gmail.com

Una API REST lista para producción diseñada para construir aplicaciones de descubrimiento y gestión de consejos prácticos. Lifehacking proporciona un backend completo para explorar consejos de vida diaria, organizarlos por categorías y gestionar favoritos de usuarios con transiciones fluidas de anónimo a autenticado.

Construido con **.NET 10** y principios de **Arquitectura Limpia**. Este proyecto fue creado a partir de [arielbvergara/clean-architecture](https://github.com/arielbvergara/clean-architecture) — una plantilla reutilizable de Arquitectura Limpia desarrollada durante lecciones y convertida en plantilla. En ese proyecto podran ver los commits que se hicieron previos a la creacion de este proyecto. 

> 🤖 Desarrollo asistido por IA: [**Kiro**](https://kiro.dev) y [**Warp**](https://warp.dev) fue utilizado como asistente de IA durante todo el desarrollo de este proyecto.


---

## 📋 Tabla de Contenidos

- [Aplicación Desplegada](#-aplicación-desplegada)
- [Slides de la presentacion del proyecto](#slides-de-la-presentacion-del-proyecto)
- [Descripción General del Proyecto](#-descripción-general-del-proyecto)
- [Stack Tecnológico](#️-stack-tecnológico)
- [Proyectos Relacionados](#-proyectos-relacionados)
- [Características Principales](#-características-principales)
- [Instalación y Ejecución](#-instalación-y-ejecución)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Endpoints de la API](#-endpoints-de-la-api)
- [Arquitectura](#-arquitectura)
- [Autenticación y Autorización](#-autenticación-y-autorización)
- [Características de Seguridad](#-características-de-seguridad)
- [Pruebas](#-pruebas)
- [Guías de Desarrollo](#-guías-de-desarrollo)
- [Roadmap](#️-roadmap)

---

## 🌐 Aplicación Desplegada

- **Frontend:** [https://lifehacking.vercel.app/](https://lifehacking.vercel.app/)
- **Backend API:** [https://slight-janet-lifehacking-ce47cbe0.koyeb.app/](https://slight-janet-lifehacking-ce47cbe0.koyeb.app/)


---

## Slides de la presentacion del proyecto

[Lifehacking Master Slides Presentation](lifehacking-master-presentation.pptx)

---

## 🎯 Descripción General del Proyecto


### ¿Qué es Lifehacking Tips API?

Lifehacking Tips API es una solución backend completa y robusta que permite construir aplicaciones donde los usuarios pueden descubrir, organizar y gestionar consejos prácticos para mejorar su vida diaria. La API está diseñada con arquitectura moderna y mejores prácticas de la industria, proporcionando una base sólida para aplicaciones web y móviles.

### Casos de Uso Principales

La API permite a los desarrolladores crear aplicaciones donde los usuarios pueden:

- **Descubrir consejos** a través de búsqueda avanzada, filtrado por categorías y etiquetas (sin necesidad de autenticación)
- **Guardar favoritos** con sincronización automática entre almacenamiento local y persistencia del lado del servidor
- **Gestionar contenido** mediante una interfaz administrativa completa para consejos y categorías
- **Administrar usuarios** con autenticación Firebase, control de acceso basado en roles y gestión de cuentas de autoservicio

### Tipos de Usuarios Soportados

El sistema está diseñado para soportar tres tipos de usuarios con diferentes niveles de acceso:

1. **Usuarios Anónimos** - Acceso completo de lectura con favoritos del lado del cliente
2. **Usuarios Autenticados** - Favoritos persistentes con fusión automática del almacenamiento local
3. **Administradores** - Capacidades completas de gestión de contenido y usuarios

### Filosofía de Diseño

El proyecto sigue los principios de **Arquitectura Limpia** (Clean Architecture) y **Diseño Dirigido por el Dominio** (Domain-Driven Design), garantizando:

- Separación clara de responsabilidades entre capas
- Independencia del dominio de negocio respecto a frameworks y tecnologías externas
- Código mantenible, testeable y escalable
- Facilidad para agregar nuevas funcionalidades sin afectar el código existente

---

## 🛠️ Stack Tecnológico

### Backend (este repositorio)


| Tecnología | Propósito |
|-----------|-----------|
| **.NET 10 + Arquitectura Limpia** | API Web con capas Domain, Application, Infrastructure y WebAPI |
| **Firebase Authentication** | Validación de tokens JWT Bearer y gestión de identidad |
| **Firebase Cloud Firestore** | Base de datos NoSQL principal |
| **AWS S3** | Almacenamiento de imágenes de categorías |
| **AWS CloudFront** | CDN para entrega de imágenes |
| **Docker & Docker Compose** | Despliegue containerizado |
| **[Koyeb](https://app.koyeb.com)** | Plataforma de despliegue en la nube |
| **Dependabot** | Actualizaciones automáticas semanales de dependencias |
| **GitHub Actions** | Pipeline CI (build, test, lint, escaneo de seguridad) y revisión de código |
| **[Kiro](https://kiro.dev)** | Asistente de IA utilizado durante el desarrollo |
| **[Warp](https://warp.dev)** | Asistente de IA utilizado durante el desarrollo |
| **[Sentry](https://sentry.io)** | Seguimiento de errores y monitoreo de rendimiento |
| **Swagger / OpenAPI** | Documentación interactiva de la API |
| **Github Copilot** | Asistente de IA utilizado para revisión de código |

### Frontend ([lifehacking-app](https://github.com/arielbvergara/lifehacking-app))

| Tecnología | Propósito |
|-----------|---------|
| **Next.js 16** | Framework frontend basado en React |
| **Google Stitch** | Diseño UI/UX |
| **Firebase Authentication** | Autenticación e identidad |
| **Vercel** | Despliegue del frontend |
| **Sentry.io** | Monitoreo y seguimiento de errores |
| **Docker** | Despliegue containerizado |
| **Dependabot** | Actualizaciones automáticas semanales de dependencias |
| **GitHub Actions** | Pipeline CI y revisión de código |
| **Kiro** | Asistente de IA utilizado durante el desarrollo |
| **Github Copilot** | Asistente de IA utilizado para revisión de código |

---

## 🔗 Proyectos Relacionados

| Proyecto | Descripción | Despliegue |
|---------|-------------|------------|
| **[lifehacking-app](https://github.com/arielbvergara/lifehacking-app)** | Frontend — Next.js 16, diseño Google Stitch, Firebase, Docker, Vercel | [Vercel](https://vercel.com) |
| **lifehacking** *(este repositorio)* | API Backend — .NET 10, Arquitectura Limpia, Firebase, Docker, AWS | [Koyeb](https://app.koyeb.com) |

---

## ✨ Características Principales


### Para Usuarios Anónimos (API Pública)

- **Exploración de consejos** con búsqueda y filtrado avanzado (por categoría, etiquetas, término de búsqueda)
- **Visualización detallada** de información de consejos incluyendo instrucciones paso a paso
- **Exploración de categorías** con acceso a todas las categorías disponibles
- **Ordenamiento flexible** de resultados por fecha de creación, fecha de actualización o título
- **Respuestas paginadas** para rendimiento óptimo
- **Gestión de favoritos del lado del cliente** (almacenamiento local)

### Para Usuarios Autenticados

- **Todas las capacidades de usuarios anónimos**
- **Favoritos persistentes** almacenados del lado del servidor
- **Fusión automática** de favoritos locales en el primer inicio de sesión (sin duplicados)
- **Sincronización entre dispositivos** de favoritos
- **Gestión de perfil de autoservicio** (ver, actualizar nombre, eliminar cuenta)

### Para Administradores

- **Todas las capacidades de usuarios autenticados**
- **Gestión completa del ciclo de vida de consejos** (crear, actualizar, eliminar)
- **Gestión de categorías** con eliminación en cascada
- **Administración de usuarios** completa
- **Creación de usuarios administradores** con integración Firebase
- **Panel de control** con estadísticas en tiempo real y conteo de entidades
- **Registro de auditoría** para todas las acciones administrativas

### Características Técnicas Destacadas

- **Arquitectura Limpia** con separación clara de responsabilidades
- **Caché en memoria** con invalidación automática para optimización de rendimiento
- **Eliminación suave (Soft Delete)** para preservación de datos y auditoría
- **Validación exhaustiva** de entrada con respuestas de error detalladas
- **IDs de correlación** para trazabilidad de solicitudes en logs y sistemas de monitoreo
- **Documentación interactiva** con Swagger/OpenAPI
- **Seguridad robusta** con JWT, rate limiting, headers de seguridad y CORS configurable

---

## 🚀 Instalación y Ejecución

### 1. Requisitos Previos

Antes de comenzar, asegúrate de tener instalado:

- [.NET SDK 10.0](https://dotnet.microsoft.com/) o superior
- [Docker](https://www.docker.com/) y Docker Compose
- Un proyecto Firebase para autenticación (créalo en [Firebase Console](https://console.firebase.google.com/))
- Opcional: Un proyecto Sentry para monitoreo (regístrate en [sentry.io](https://sentry.io/))


### 2. Inicio Rápido con Docker Compose

La forma más rápida de ejecutar la API localmente con todas las dependencias configuradas.

#### Requisitos Previos para Docker Compose

Antes de ejecutar `docker compose up`, necesitas:

1. **Archivo de credenciales de Firebase Admin SDK**
   - Descarga el archivo JSON de credenciales desde [Firebase Console](https://console.firebase.google.com/)
   - Ve a: Configuración del proyecto → Cuentas de servicio → Generar nueva clave privada
   - Guarda el archivo como `firebase-adminsdk.json` en `~/secrets/`
   
   ```bash
   # Crear directorio si no existe
   mkdir -p ~/secrets
   
   # Copiar tu archivo de credenciales
   cp /ruta/a/tu/firebase-adminsdk.json ~/secrets/firebase-adminsdk.json
   ```

2. **Configurar variables de entorno en docker-compose.yml** (ya configuradas por defecto):
   - `ASPNETCORE_ENVIRONMENT: Development` - Entorno de desarrollo
   - `ClientApp__Origin: "http://localhost:3000"` - Origen del frontend para CORS
   - `GOOGLE_APPLICATION_CREDENTIALS: /app/firebase-adminsdk.json` - Ruta a credenciales dentro del contenedor

3. **Configurar Firebase en appsettings.json o variables de entorno**
   
   Opción A: Editar `lifehacking/WebAPI/appsettings.Development.json`:
   ```json
   {
     "Firebase": {
       "ProjectId": "tu-proyecto-firebase",
     },
     "Authentication": {
       "Authority": "https://securetoken.google.com/tu-proyecto-firebase",
       "Audience": "tu-proyecto-firebase"
     }
   }
   ```
   
   Opción B: Agregar variables de entorno en `docker-compose.yml`:
   ```yaml
   environment:
     Firebase__ProjectId: "tu-proyecto-firebase"
     Authentication__Authority: "https://securetoken.google.com/tu-proyecto-firebase"
     Authentication__Audience: "tu-proyecto-firebase"
   ```

#### Ejecutar con Docker Compose

Una vez configurado:

```bash
docker compose up --build
```

Esto realizará:
- Construcción de la imagen Docker con .NET 10
- Montaje del archivo de credenciales Firebase
- Inicio del contenedor WebAPI
- Configuración de la API para usar Firebase/Firestore
- Exposición de la API en el puerto 8080

Una vez en ejecución:
- **URL Base de la API**: `http://localhost:8080`
- **Swagger UI**: `http://localhost:8080/swagger` (documentación interactiva de la API)
- **Health Check**: `http://localhost:8080/health` (si está configurado)

Para detener los servicios:

```bash
docker compose down
```

**Nota importante:** El proyecto está diseñado para usar Firebase/Firestore como base de datos. Docker Compose está configurado para conectarse automáticamente a tu proyecto Firebase.

### 3. Ejecutar la WebAPI Directamente (Sin Docker)

Para iteración más rápida durante el desarrollo, ejecuta la API directamente usando el SDK de .NET:

```bash
# Compilar la solución
dotnet build lifehacking.slnx

# Ejecutar el proyecto WebAPI
dotnet run --project lifehacking/WebAPI/WebAPI.csproj
```

La API lee la configuración de `lifehacking/WebAPI/appsettings.Development.json` y variables de entorno, conectándose a Firebase/Firestore según la configuración establecida.

### 4. Configurar Autenticación Firebase

Para probar endpoints autenticados y de administrador, configura Firebase como tu proveedor de identidad:

1. **Actualiza `appsettings.Development.json`**:
   ```json
   {
     "Authentication": {
       "Authority": "https://securetoken.google.com/<tu-firebase-project-id>",
       "Audience": "<tu-firebase-project-id>"
     },
     "Firebase": {
       "ProjectId": "<tu-firebase-project-id>"
     }
   }
   ```

2. **Obtén un token de ID de Firebase**:
   - Autentica un usuario a través de Firebase (web, móvil o API REST)
   - Extrae el token de ID de la respuesta de autenticación

3. **Usa el token en las solicitudes de la API**:
   ```bash
   curl -H "Authorization: Bearer <firebase-id-token>" \
        http://localhost:8080/api/user/me
   ```

La API valida el token JWT y mapea el claim `sub` al `ExternalAuthId` del usuario interno.


### 5. Configurar Monitoreo con Sentry (Opcional)

La integración con Sentry es opcional. La API funciona normalmente con Sentry deshabilitado.

Para habilitar el monitoreo, establece estas variables de entorno:

```bash
export Sentry__Enabled=true
export Sentry__Dsn=<tu-sentry-dsn>
export Sentry__Environment=Development
export Sentry__TracesSampleRate=0.2
```

O configura en `appsettings.Development.json`:

```json
{
  "Sentry": {
    "Enabled": true,
    "Dsn": "<tu-sentry-dsn>",
    "Environment": "Development",
    "TracesSampleRate": 0.2
  }
}
```

Cuando está habilitado, los errores no manejados y las trazas de rendimiento se envían a Sentry con contexto completo (ruta, usuario, ID de correlación).

### 6. Explorar la API con Swagger

Una vez que la API esté en ejecución, navega a la interfaz Swagger para documentación interactiva:

**http://localhost:8080/swagger**

Swagger proporciona:
- Documentación completa de endpoints con esquemas de solicitud/respuesta
- Reglas de validación y restricciones
- Pruebas interactivas (prueba endpoints directamente desde el navegador)
- Soporte de autenticación (agrega tu token Bearer para probar endpoints protegidos)

Nota: Swagger esta habilitado solo en ambientes no productivos.

### 7. Configurar AWS S3 para Carga de Imágenes

Para habilitar la carga de imágenes de categorías, configura AWS S3 y CloudFront:

```bash
export AWS_ACCESS_KEY_ID=tu-access-key-id
export AWS_SECRET_ACCESS_KEY=tu-secret-access-key
export AWS_REGION=us-east-1
export AWS__S3__BucketName=lifehacking-category-images
export AWS__CloudFront__Domain=tu-distribucion.cloudfront.net
```

Para instrucciones detalladas de configuración de AWS, consulta **[docs/AWS-S3-Setup-Guide.md](docs/AWS-S3-Setup-Guide.md)**

---

## 📁 Estructura del Proyecto


El proyecto sigue los principios de **Arquitectura Limpia** con una clara separación de responsabilidades:

```
lifehacking/
├── lifehacking.slnx                 # Archivo de solución .NET 10
├── README.md                        # Documentación principal (inglés)
├── AGENTS.md                        # Guía para agentes de IA
├── docker-compose.yml               # Configuración Docker Compose
├── Dockerfile                       # Imagen Docker de la aplicación
│
├── ADRs/                            # Architecture Decision Records
│   ├── 001-use-microsoft-testing-platform-runner.md
│   ├── 018-replace-postgresql-persistence-with-firebase-database.md
│   ├── 020-user-favorites-domain-model-and-storage.md
│   └── ...                          # Más decisiones arquitectónicas
│
├── docs/                            # Documentación adicional
│   ├── MVP.md                       # Requisitos del producto y alcance MVP
│   ├── AWS-S3-Setup-Guide.md        # Guía de configuración AWS S3
│   └── Search-Architecture-Decision.md
│
└── lifehacking/                     # Código fuente principal
    │
    ├── Domain/                      # Capa de Dominio
    │   ├── Entities/                # Entidades del dominio (User, Tip, Category, UserFavorites)
    │   ├── ValueObject/             # Objetos de valor (CategoryImage, etc.)
    │   ├── Primitives/              # Tipos primitivos (Result<T, TE>)
    │   └── Constants/               # Constantes del dominio (ImageConstants)
    │
    ├── Application/                 # Capa de Aplicación
    │   ├── UseCases/                # Casos de uso organizados por característica
    │   │   ├── User/                # Casos de uso de usuarios
    │   │   ├── Category/            # Casos de uso de categorías
    │   │   ├── Tip/                 # Casos de uso de consejos
    │   │   ├── Favorite/            # Casos de uso de favoritos
    │   │   └── Dashboard/           # Casos de uso del panel de control
    │   ├── Dtos/                    # Objetos de transferencia de datos
    │   │   ├── User/                # DTOs de usuarios
    │   │   ├── Category/            # DTOs de categorías
    │   │   ├── Tip/                 # DTOs de consejos
    │   │   ├── Favorite/            # DTOs de favoritos
    │   │   └── Dashboard/           # DTOs del panel de control
    │   ├── Interfaces/              # Interfaces (puertos)
    │   │   ├── IUserRepository
    │   │   ├── ICategoryRepository
    │   │   ├── IImageStorageService
    │   │   └── ICacheInvalidationService
    │   ├── Exceptions/              # Excepciones de aplicación
    │   ├── Validation/              # Utilidades de validación
    │   └── Caching/                 # Definiciones de claves de caché
    │
    ├── Infrastructure/              # Capa de Infraestructura
    │   ├── Data/Firestore/          # Implementación Firestore
    │   │   ├── Documents/           # Clases de documentos Firestore
    │   │   └── DataStores/          # Almacenes de datos (mapeo entidad-documento)
    │   ├── Repositories/            # Implementaciones de repositorios
    │   ├── Storage/                 # Servicios de almacenamiento en la nube
    │   │   └── S3ImageStorageService.cs
    │   └── Configuration/           # Clases de opciones de configuración
    │
    ├── WebAPI/                      # Capa de API Web
    │   ├── Program.cs               # Punto de entrada y composición raíz
    │   ├── Controllers/             # Controladores REST
    │   │   ├── UserController.cs
    │   │   ├── AdminCategoryController.cs
    │   │   ├── AdminDashboardController.cs
    │   │   └── ...
    │   ├── Filters/                 # Filtros globales
    │   │   └── GlobalExceptionFilter.cs
    │   ├── Configuration/           # Configuración de servicios
    │   ├── appsettings.json         # Configuración base
    │   ├── appsettings.Development.json
    │   └── appsettings.Production.json
    │
    └── Tests/                       # Proyectos de pruebas
        ├── Application.Tests/       # Pruebas de la capa de aplicación
        ├── Infrastructure.Tests/    # Pruebas de la capa de infraestructura
        └── WebAPI.Tests/            # Pruebas de integración de la API

```

### Dirección de Dependencias

El proyecto sigue estrictamente las reglas de dependencia de Arquitectura Limpia:

- **Domain** → Sin referencias a otros proyectos (completamente independiente)
- **Application** → Depende solo de **Domain**
- **Infrastructure** → Depende de **Application** y **Domain**
- **WebAPI** → Depende de **Application**, **Domain** e **Infrastructure**
- **Tests** → Referencian solo las capas que están destinados a validar

### Flujo de Solicitudes HTTP

```
Cliente HTTP
    ↓
WebAPI Controller (capa de presentación)
    ↓
Application Use Case (lógica de negocio)
    ↓
Domain Entities/Value Objects (modelo de dominio)
    ↓
Infrastructure Repository (acceso a datos)
    ↓
Firestore/Firebase (persistencia)
    ↓
Result<T, AppException> (respuesta)
    ↓
HTTP Response (mapeo a códigos de estado)
```

---

## 🔌 Endpoints de la API


Todos los endpoints devuelven JSON y siguen RFC 7807 Problem Details para respuestas de error. Cada respuesta incluye un `correlationId` para trazabilidad de solicitudes.

Para esquemas completos de solicitud/respuesta, reglas de validación y pruebas interactivas, consulta la **Swagger UI** en `http://localhost:8080/swagger` cuando ejecutes la API.

### Endpoints Públicos (Sin Autenticación Requerida)

#### API de Consejos - `/api/tip`

- **`GET /api/tip`** - Buscar y filtrar consejos
  - Parámetros de consulta: `q` (término de búsqueda), `categoryId`, `tags[]`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Devuelve resúmenes de consejos paginados con metadatos
  
- **`GET /api/tip/{id}`** - Obtener detalles completos de un consejo
  - Devuelve consejo completo con título, descripción, pasos ordenados, categoría, etiquetas y URL de video opcional

#### API de Categorías - `/api/category`

- **`GET /api/category`** - Listar todas las categorías disponibles
  - Devuelve todas las categorías no eliminadas
  
- **`GET /api/category/{id}/tips`** - Obtener consejos por categoría
  - Parámetros de consulta: `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Devuelve consejos paginados para la categoría especificada

### Endpoints Autenticados (Requiere Token JWT Bearer)

#### API de Usuario - `/api/user`

- **`POST /api/user`** - Crear perfil de usuario después de la autenticación
  - Se llama una vez después de la autenticación Firebase para crear el registro de usuario interno
  - ID de autenticación externa derivado del token JWT
  
- **`GET /api/user/me`** - Obtener perfil del usuario actual
  - Usuario resuelto desde el token JWT
  
- **`PUT /api/user/me/name`** - Actualizar nombre de visualización del usuario actual
  - Actualización de perfil de autoservicio
  
- **`DELETE /api/user/me`** - Eliminar cuenta del usuario actual
  - Eliminación suave con registro de auditoría

#### API de Favoritos - `/api/me/favorites`

- **`GET /api/me/favorites`** - Listar consejos favoritos del usuario
  - Parámetros de consulta: `q`, `categoryId`, `tags[]`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`
  - Devuelve favoritos paginados con detalles completos del consejo
  
- **`POST /api/me/favorites/{tipId}`** - Agregar consejo a favoritos
  - Operación idempotente
  
- **`DELETE /api/me/favorites/{tipId}`** - Eliminar consejo de favoritos

- **`POST /api/me/favorites/merge`** - Fusionar favoritos locales del almacenamiento del cliente
  - Acepta array de IDs de consejos del almacenamiento local
  - Devuelve resumen con conteos de agregados, omitidos y fallidos
  - Idempotente y soporta éxito parcial


### Endpoints de Administrador (Requiere Rol de Admin)

#### API de Consejos de Admin - `/api/admin/tips`

- **`POST /api/admin/tips`** - Crear nuevo consejo
  - Requerido: title, description, steps (lista ordenada), categoryId
  - Opcional: tags (máx 10), videoUrl (YouTube/Instagram)
  
- **`PUT /api/admin/tips/{id}`** - Actualizar consejo existente
  - Todos los campos actualizables
  
- **`DELETE /api/admin/tips/{id}`** - Eliminación suave de consejo
  - Marca el consejo como eliminado, preserva los datos

#### API de Categorías de Admin - `/api/admin/categories`

- **`POST /api/admin/categories/images`** - Subir imagen de categoría
  - Acepta multipart/form-data con archivo de imagen
  - Valida tamaño de archivo (máx 5MB), tipo de contenido (JPEG, PNG, GIF, WebP) y bytes mágicos
  - Sube a AWS S3 con nombre de archivo único basado en GUID
  - Devuelve metadatos de imagen incluyendo URL de CDN CloudFront
  - Requerido para crear categorías con imágenes
  
- **`POST /api/admin/categories`** - Crear nueva categoría
  - Requerido: name (2-100 caracteres, único sin distinción de mayúsculas)
  - Opcional: metadatos de imagen del endpoint de carga
  
- **`PUT /api/admin/categories/{id}`** - Actualizar nombre de categoría
  - Aplica unicidad
  
- **`DELETE /api/admin/categories/{id}`** - Eliminación suave de categoría
  - Cascada de eliminación suave a todos los consejos asociados

#### API de Usuarios de Admin - `/api/admin/user`

- **`POST /api/admin/user`** - Crear usuario administrador
  - Crea usuario en Firebase y base de datos interna
  - Requerido: email, displayName, password
  
- **`GET /api/admin/user`** - Listar usuarios con paginación
  - Parámetros de consulta: `search`, `orderBy`, `sortDirection`, `pageNumber`, `pageSize`, `isDeleted`
  - Soporta búsqueda en email, nombre e ID
  
- **`GET /api/admin/user/{id}`** - Obtener usuario por ID interno

- **`GET /api/admin/user/email/{email}`** - Obtener usuario por dirección de email

- **`PUT /api/admin/user/{id}/name`** - Actualizar nombre de visualización del usuario

- **`DELETE /api/admin/user/{id}`** - Eliminación suave de cuenta de usuario

#### API de Panel de Control de Admin - `/api/admin/dashboard`

- **`GET /api/admin/dashboard`** - Obtener estadísticas del panel de control
  - Devuelve conteos de entidades para usuarios, categorías y consejos
  - Resultados en caché durante 1 hora para rendimiento óptimo
  - Proporciona vista rápida para monitoreo administrativo

---

## 🏗️ Arquitectura


Esta API sigue los principios de **Arquitectura Limpia** (Clean Architecture) con clara separación de responsabilidades:

### Capas de la Arquitectura

#### 1. Capa de Dominio (Domain Layer)

**Responsabilidad:** Contiene la lógica de negocio central y las reglas del dominio.

**Características:**
- Entidades del negocio (User, Tip, Category, UserFavorites)
- Objetos de valor (CategoryImage)
- Tipos primitivos del dominio (Result<T, TE>)
- Constantes del dominio (ImageConstants)
- Sin dependencias externas (completamente independiente)
- Agnóstico de persistencia y frameworks

**Principio:** El dominio es el corazón de la aplicación y no debe depender de nada externo.

#### 2. Capa de Aplicación (Application Layer)

**Responsabilidad:** Orquesta los casos de uso y coordina el flujo de datos.

**Características:**
- Casos de uso organizados por característica (User, Category, Tip, Favorite, Dashboard)
- DTOs (Data Transfer Objects) para comunicación con la capa de presentación
- Interfaces (puertos) para servicios externos (IUserRepository, ICategoryRepository, IImageStorageService)
- Lógica de validación y transformación
- Gestión de caché con invalidación automática
- Manejo de excepciones de aplicación

**Principio:** Define qué hace el sistema sin preocuparse por cómo lo hace.

#### 3. Capa de Infraestructura (Infrastructure Layer)

**Responsabilidad:** Implementa los detalles técnicos y servicios externos.

**Características:**
- Implementaciones de repositorios (UserRepository, CategoryRepository)
- Acceso a datos con Firestore (documentos, data stores)
- Servicios de almacenamiento en la nube (S3ImageStorageService)
- Integración con Firebase Authentication
- Configuración de servicios externos (AWS, Firebase)
- Mapeo entre entidades de dominio y documentos de persistencia

**Principio:** Proporciona las implementaciones concretas de las abstracciones definidas en Application.

#### 4. Capa de API Web (WebAPI Layer)

**Responsabilidad:** Expone la funcionalidad a través de endpoints HTTP REST.

**Características:**
- Controladores REST organizados por característica
- Middleware de autenticación y autorización
- Filtros globales (GlobalExceptionFilter)
- Configuración de servicios y composición raíz (Program.cs)
- Documentación Swagger/OpenAPI
- Mapeo de Result<T, AppException> a códigos de estado HTTP

**Principio:** Capa delgada enfocada en preocupaciones HTTP, delegando lógica a Application.

### Patrones Arquitectónicos Aplicados

#### Patrón Result

En lugar de lanzar excepciones para flujo de control normal, se usa el patrón Result:

```csharp
Result<TipDetailResponse, AppException> result = await useCase.ExecuteAsync(request);

return result.Match(
    success => Ok(success),
    error => error.ToActionResult()
);
```

**Beneficios:**
- Manejo explícito de errores
- Mejor rendimiento (sin stack unwinding)
- Código más predecible y testeable

#### Inyección de Dependencias

Todas las dependencias se inyectan a través de constructores:

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

**Beneficios:**
- Facilita testing con mocks
- Bajo acoplamiento
- Fácil sustitución de implementaciones

#### Repository Pattern

Abstrae el acceso a datos detrás de interfaces:

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

**Beneficios:**
- Independencia de la tecnología de persistencia
- Facilita cambios de base de datos
- Mejora testabilidad

### Decisiones Arquitectónicas Documentadas

Las decisiones arquitectónicas clave están documentadas en `ADRs/`:

- **ADR-018** - Reemplazo de PostgreSQL por Firebase Firestore
- **ADR-020** - Modelo de dominio y almacenamiento de favoritos de usuario
- **ADR-006** - Roles de usuario y ciclo de vida de eliminación suave
- **ADR-010** - Configuración de producción endurecida
- **ADR-011** - Headers de seguridad y rate limiting
- **ADR-013** - Manejo estandarizado de errores y logging de seguridad
- **ADR-015** - Integración de monitoreo y observabilidad con Sentry

---

## 🔐 Autenticación y Autorización


### Flujo de Autenticación

El sistema utiliza Firebase Authentication con tokens JWT Bearer:

1. **Usuario se autentica con Firebase** (tu frontend maneja esto)
2. **Frontend recibe token de ID de Firebase** (JWT)
3. **Frontend llama a la API con el token** en el header `Authorization: Bearer <token>`
4. **API valida el token** con Firebase y extrae la identidad del usuario
5. **API mapea el UID de Firebase** al registro de usuario interno

### Tipos de Usuarios y Permisos

#### Usuarios Anónimos
- **Acceso:** Sin autenticación requerida
- **Permisos:**
  - Lectura completa de consejos y categorías
  - Búsqueda y filtrado avanzado
  - Gestión de favoritos del lado del cliente (local storage)

#### Usuarios Autenticados
- **Acceso:** Requiere token JWT válido
- **Permisos:**
  - Todos los permisos de usuarios anónimos
  - Favoritos persistentes del lado del servidor
  - Gestión de perfil (ver, actualizar nombre)
  - Eliminación de cuenta (autoservicio)
  - Fusión de favoritos locales

#### Administradores
- **Acceso:** Requiere token JWT válido con rol Admin
- **Permisos:**
  - Todos los permisos de usuarios autenticados
  - Gestión completa de consejos (crear, actualizar, eliminar)
  - Gestión de categorías (crear, actualizar, eliminar, subir imágenes)
  - Administración de usuarios (crear, listar, actualizar, eliminar)
  - Acceso al panel de control con estadísticas

### Registro de Usuario por Primera Vez

Después de la autenticación con Firebase, los usuarios deben crear su perfil interno:

```bash
POST /api/user
Authorization: Bearer <firebase-id-token>
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "name": "Juan Pérez"
}
```

El `ExternalAuthId` se extrae automáticamente del token JWT (claim `sub`).

### Bootstrap de Administrador

Los administradores pueden crearse mediante:

1. **Seeding en inicio** - Configura `AdminUser:SeedOnStartup=true` con credenciales en variables de entorno
2. **API de Admin** - Administradores existentes pueden crear nuevos admins vía `POST /api/admin/user`

**Ejemplo de configuración para seeding:**

```json
{
  "AdminUser": {
    "SeedOnStartup": true,
    "Email": "admin@ejemplo.com",
    "DisplayName": "Administrador",
    "Password": "ContraseñaSegura123!"
  }
}
```

### Validación de Tokens JWT

La API valida automáticamente los tokens JWT usando la configuración de Firebase:

```json
{
  "Authentication": {
    "Authority": "https://securetoken.google.com/<tu-project-id>",
    "Audience": "<tu-project-id>"
  }
}
```

**Claims importantes del JWT:**
- `sub` - UID de Firebase (mapeado a ExternalAuthId)
- `email` - Email del usuario
- `email_verified` - Estado de verificación del email
- `role` - Rol personalizado (User o Admin)

---

## 🛡️ Características de Seguridad


Esta API está lista para producción con medidas de seguridad exhaustivas:

### Autenticación y Autorización

- **JWT Authentication** - Validación de tokens basada en Firebase con autorización basada en roles
- **Role-Based Access Control (RBAC)** - Separación clara entre usuarios anónimos, autenticados y administradores
- **Token Validation** - Validación automática de firma, expiración y audiencia de tokens JWT
- **Secure Claims Mapping** - Mapeo seguro de claims JWT a identidad de usuario interno

### Rate Limiting (Limitación de Tasa)

Dos políticas de rate limiting para proteger contra abuso:

#### Política Fixed (Fija)
- **Límite:** 100 solicitudes por minuto
- **Aplicado a:** Endpoints estándar de lectura y escritura
- **Ventana:** 1 minuto deslizante

#### Política Strict (Estricta)
- **Límite:** 10 solicitudes por minuto
- **Aplicado a:** Operaciones sensibles (crear, actualizar, eliminar)
- **Ventana:** 1 minuto deslizante

**Respuesta cuando se excede el límite:**
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

### Headers de Seguridad

La API configura automáticamente headers de seguridad HTTP:

- **Content-Security-Policy (CSP)** - Previene ataques XSS
- **Strict-Transport-Security (HSTS)** - Fuerza conexiones HTTPS
- **X-Frame-Options** - Previene clickjacking
- **X-Content-Type-Options** - Previene MIME sniffing
- **Referrer-Policy** - Controla información de referrer
- **Permissions-Policy** - Controla características del navegador

### CORS (Cross-Origin Resource Sharing)

Configuración CORS flexible para integración con frontend:

```json
{
  "ClientApp": {
    "Origin": "https://tu-app.com"
  }
}
```

**Características:**
- Orígenes configurables por entorno
- Soporte para múltiples orígenes en producción
- Headers permitidos específicos
- Métodos HTTP permitidos controlados

### Validación de Entrada

Validación exhaustiva en múltiples niveles:

#### Validación de DTOs
- Anotaciones de datos en DTOs
- Validación automática en el pipeline de ASP.NET Core
- Mensajes de error descriptivos

#### Validación de Dominio
- Reglas de negocio en entidades
- Objetos de valor con validación incorporada
- Validación de invariantes del dominio

#### Validación de Archivos
- **Magic Byte Validation** - Previene spoofing de tipo de contenido
- **Sanitización de nombres de archivo** - Previene vulnerabilidades de path traversal
- **Validación de tamaño** - Límites definidos en constantes (máx 5MB para imágenes)
- **Validación de tipo MIME** - Solo tipos permitidos (JPEG, PNG, GIF, WebP)

### Soft Delete (Eliminación Suave)

Preservación de datos con registro de auditoría:

- **Usuarios** - Marcados como eliminados, datos preservados
- **Consejos** - Marcados como eliminados, relaciones preservadas
- **Categorías** - Eliminación en cascada suave a consejos relacionados
- **Auditoría** - Timestamps de eliminación para trazabilidad

### Logging y Auditoría

Sistema completo de logging con integración Sentry:

- **Correlation IDs** - Trazabilidad de solicitudes en todos los logs
- **Structured Logging** - Logs estructurados con contexto rico
- **Security Events** - Logging de eventos de seguridad (autenticación, autorización)
- **Error Tracking** - Captura automática de excepciones no manejadas
- **Performance Monitoring** - Trazas de rendimiento con sample rate configurable

### Manejo de Errores Estandarizado

Respuestas de error consistentes siguiendo RFC 7807:

```csharp
{
  "status": 400,
  "type": "https://httpstatuses.io/400/validation-error",
  "title": "Validation error",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/admin/tips",
  "correlationId": "abc123",
  "errors": {
    "Title": ["El título del consejo debe tener al menos 5 caracteres"]
  }
}
```

**Beneficios:**
- Formato estándar de la industria
- Información de error detallada sin exponer detalles de implementación
- Correlation IDs para soporte y debugging
- Respuestas consistentes en toda la API

### Protección contra Vulnerabilidades Comunes

- **SQL Injection** - No aplicable (NoSQL con Firestore)
- **XSS (Cross-Site Scripting)** - Headers CSP y sanitización de entrada
- **CSRF (Cross-Site Request Forgery)** - Tokens JWT stateless
- **Path Traversal** - Sanitización de nombres de archivo
- **Content Type Spoofing** - Validación de magic bytes
- **Denial of Service** - Rate limiting y timeouts configurables
- **Information Disclosure** - Mensajes de error genéricos en producción

---

## 🧪 Pruebas


El proyecto incluye cobertura de pruebas exhaustiva en todas las capas:

### Proyectos de Pruebas

- **Application.Tests** - Pruebas de casos de uso y lógica de dominio
- **Infrastructure.Tests** - Pruebas de repositorios y acceso a datos con emulador Firestore
- **WebAPI.Tests** - Pruebas de integración para controladores y middleware

### Ejecutar Pruebas

```bash
# Ejecutar todas las pruebas
dotnet test lifehacking.slnx

# Ejecutar pruebas de un proyecto específico
dotnet test lifehacking/Tests/Application.Tests/Application.Tests.csproj
dotnet test lifehacking/Tests/Infrastructure.Tests/Infrastructure.Tests.csproj
dotnet test lifehacking/Tests/WebAPI.Tests/WebAPI.Tests.csproj

# Ejecutar una prueba específica
dotnet test --filter "Name=CreateTip_ShouldReturnValidationError_WhenTitleIsTooShort"

# Ejecutar todas las pruebas de una clase
dotnet test --filter "FullyQualifiedName~CreateTipUseCaseTests"
```

### Enfoque de Pruebas

#### Microsoft Testing Platform

El proyecto utiliza **Microsoft Testing Platform** como runner de pruebas moderno:

```xml
<PropertyGroup>
  <DotNetTestRunner>Microsoft.Testing.Platform</DotNetTestRunner>
</PropertyGroup>
```

**Beneficios:**
- Rendimiento mejorado
- Mejor integración con herramientas de desarrollo
- Soporte para pruebas basadas en propiedades

#### xUnit Framework

Todas las pruebas usan xUnit como framework de testing:

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

Sintaxis de aserciones expresiva y legible:

```csharp
// En lugar de
Assert.Equal(expected, actual);
Assert.True(condition);

// Usamos
actual.Should().Be(expected);
condition.Should().BeTrue();
result.Should().NotBeNull();
list.Should().HaveCount(5);
```

**Beneficios:**
- Mensajes de error más descriptivos
- Sintaxis más natural y legible
- Mejor experiencia de desarrollo


#### Emulador de Firestore

Las pruebas de infraestructura usan el emulador local de Firestore:

```bash
# Iniciar emulador
firebase emulators:start --only firestore

# Las pruebas se conectan automáticamente al emulador
export FIRESTORE_EMULATOR_HOST=localhost:8080
```

**Beneficios:**
- Pruebas de integración realistas
- Sin costos de Firebase
- Datos aislados por ejecución de prueba
- Velocidad de ejecución rápida

### Convención de Nombres de Pruebas

Todas las pruebas siguen el patrón:

```
{NombreDelMétodo}_Should{HacerAlgo}_When{Condición}
```

**Ejemplos:**
```csharp
CreateTip_ShouldReturnSuccess_WhenDataIsValid()
CreateTip_ShouldReturnValidationError_WhenTitleIsTooShort()
GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
AddFavorite_ShouldBeIdempotent_WhenCalledMultipleTimes()
```

**Beneficios:**
- Nombres autodescriptivos
- Fácil identificación de escenarios
- Documentación viva del comportamiento

### Organización de Pruebas

Las pruebas están organizadas por característica y capa:

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

### Estrategias de Testing

#### Pruebas Unitarias (Application.Tests)

- Prueban casos de uso aislados
- Usan mocks para dependencias (repositorios, servicios)
- Verifican lógica de negocio y validación
- Prueban comportamiento de caché

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

#### Pruebas de Integración (Infrastructure.Tests)

- Prueban repositorios con emulador Firestore
- Verifican mapeo entre entidades y documentos
- Prueban consultas y filtros complejos
- Validan comportamiento de persistencia

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

#### Pruebas de API (WebAPI.Tests)

- Prueban endpoints HTTP completos
- Verifican códigos de estado y respuestas
- Validan autenticación y autorización
- Prueban manejo de errores

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

### Cobertura de Pruebas

El proyecto mantiene alta cobertura de pruebas:

- **Casos de uso:** >90% de cobertura
- **Repositorios:** >85% de cobertura
- **Controladores:** >80% de cobertura
- **Lógica de dominio:** 100% de cobertura

Para generar reporte de cobertura:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 📚 Guías de Desarrollo


### Para Agentes de IA y Desarrolladores

El proyecto incluye documentación exhaustiva para facilitar el desarrollo:

- **AGENTS.md** - Guía completa para agentes de IA trabajando con este código
- **ADRs/** - Architecture Decision Records documentando decisiones técnicas clave
- **docs/MVP.md** - Requisitos del producto y alcance del MVP
- **docs/AWS-S3-Setup-Guide.md** - Guía detallada de configuración de AWS S3

### Estándares de Código

#### Arquitectura Limpia

- Mantén dependencias estrictas entre capas
- Domain no debe depender de nada
- Application solo depende de Domain
- Infrastructure implementa interfaces de Application
- WebAPI es la capa de composición

#### Diseño Dirigido por Dominio (DDD)

- Usa entidades ricas con comportamiento
- Encapsula lógica de negocio en el dominio
- Usa objetos de valor para conceptos sin identidad
- Mantén agregados consistentes

#### Sin Valores Mágicos

```csharp
// ❌ Incorrecto
if (file.Length > 5242880) { /* ... */ }

// ✅ Correcto
if (file.Length > ImageConstants.MaxFileSizeInBytes) { /* ... */ }
```

**Reglas:**
- Define constantes con nombres significativos
- Centraliza valores reutilizables
- Usa enums para conjuntos de valores relacionados
- Nombres autodescriptivos que expresen intención

#### Validación Exhaustiva

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

#### Patrón Result para Manejo de Errores

```csharp
// En lugar de lanzar excepciones
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

#### Soft Delete para Preservación de Datos

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

### Convenciones de Commits

Sigue [Conventional Commits](https://www.conventionalcommits.org/):

```
<tipo>: <descripción>

<cuerpo opcional>

<footer opcional>
```

**Tipos permitidos:**
- `feat` - Nueva funcionalidad
- `fix` - Corrección de bug
- `chore` - Tareas de mantenimiento
- `refactor` - Refactorización de código
- `docs` - Cambios en documentación
- `test` - Agregar o modificar pruebas

**Ejemplos:**
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

### Estrategia de Branching

- **Ramas de características:** `issue-<ticket-id>-<descripcion-corta>`
- **Sin commits directos** a la rama principal
- **Pull requests requeridos** para todos los cambios
- **Revisión de código** antes de merge

**Ejemplo:**
```bash
# Crear rama desde issue
git checkout -b issue-123-add-favorites-merge

# Hacer commits
git commit -m "feat: add merge favorites use case"
git commit -m "test: add merge favorites tests"

# Push y crear PR
git push origin issue-123-add-favorites-merge
```
---

## 🗺️ Roadmap

Características planificadas para futuras versiones:

### v2.0
- [ ] Búsqueda de texto completo con Algolia o Elasticsearch
- [ ] Sistema de comentarios y valoraciones de consejos
- [ ] Notificaciones push para nuevos consejos

### v2.1
- [ ] Soporte multiidioma para consejos
- [ ] Recomendaciones personalizadas basadas en IA
- [ ] Integración con redes sociales para compartir
- [ ] Estadísticas avanzadas para administradores

### v3.0
- [ ] Aplicación móvil nativa (iOS y Android)
- [ ] Modo offline con sincronización
- [ ] Gamificación (badges, logros, niveles)
- [ ] Comunidad de usuarios con perfiles públicos