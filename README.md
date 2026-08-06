# LoginLab

Laboratorio práctico de autenticación y gestión de identidad construido con **ASP.NET Core MVC** y **PostgreSQL**. El objetivo de este proyecto es implementar, desde cero y sin frameworks de autenticación "todo en uno" (como ASP.NET Core Identity, Auth0 o Firebase Auth), los mecanismos de seguridad que normalmente quedan ocultos detrás de esas librerías: hashing de contraseñas, gestión de sesiones con revocación real, verificación de email, segundo factor de autenticación, y más.

> Este es un proyecto de aprendizaje. Las decisiones técnicas están documentadas y razonadas a propósito, para que cada pieza sea un punto de partida para entender **por qué** se hace así y no solo **cómo**.

## Stack técnico

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Base de datos**: PostgreSQL
- **ORM**: Entity Framework Core
- **Hashing de contraseñas**: Argon2id
- **Validación**: FluentValidation
- **Email (desarrollo)**: Mailhog (servidor SMTP simulado)
- **Contenedores**: Docker Compose (PostgreSQL + Mailhog)

## Arquitectura

El proyecto sigue una arquitectura en capas (inspirada en Clean Architecture), pensada para mantener la lógica de negocio aislada de detalles de infraestructura como EF Core, PostgreSQL o el propio framework MVC:

```
LoginLab.sln
│
├── LoginLab.Web              → Presentación: Controllers, Views, ViewModels (MVC)
├── LoginLab.Application       → Lógica de negocio: Services, DTOs, Interfaces, Validators
├── LoginLab.Domain            → Entidades de dominio puras, sin dependencias externas
└── LoginLab.Infrastructure    → EF Core, repositorios, hashing, envío de emails
```

**Regla de dependencias**: `Domain` no depende de nada. `Application` solo depende de `Domain`. `Infrastructure` implementa las interfaces definidas en `Application`. `Web` orquesta, pero no contiene lógica de negocio.

Esta separación permite, por ejemplo, testear toda la lógica de autenticación sin levantar un servidor HTTP ni una base de datos real, y sustituir piezas (como el proveedor de email o el motor de base de datos) sin tocar el resto del sistema.

## Funcionalidades implementadas

### ✅ Fase 1 — Registro y login básico
- Registro de usuarios con hash de contraseña mediante **Argon2id** (salt aleatorio, parámetros de coste configurables y almacenados junto al hash)
- Login con verificación de credenciales y mensajes de error genéricos (mitigación de enumeración de usuarios)
- Gestión de sesiones **híbrida**: cookies de autenticación de ASP.NET Core + tabla `sessions` en PostgreSQL, con validación en cada petición mediante el evento `OnValidatePrincipal`. Esto permite **revocación real e instantánea** de sesiones (a diferencia de una cookie autocontenida o un JWT sin blacklist)
- Middleware `[Authorize]` protegiendo rutas privadas
- Logout que revoca la sesión en base de datos, no solo borra la cookie del navegador
- Manejo específico de condiciones de carrera en el registro (violación de restricción única `23505` de PostgreSQL, traducida a una excepción de dominio propia)

### ✅ Fase 2 — Políticas de contraseña y verificación de email
- Validación de contraseñas alineada con **NIST 800-63B**: longitud mínima (12 caracteres) en lugar de reglas de complejidad forzada, que están desaconsejadas por llevar a patrones predecibles
- Comprobación de contraseñas filtradas contra la API de **Have I Been Pwned**, usando el protocolo de **k-anonimato** (solo se envía un prefijo de 5 caracteres del hash SHA-1, nunca la contraseña ni el hash completo)
- Flujo de verificación de email con tokens de un solo uso: generados con `RandomNumberGenerator` (256 bits de entropía), almacenados **hasheados** (SHA-256) en base de datos, con expiración de 30 minutos
- Bloqueo de login para cuentas con email no verificado
- Reenvío de email de verificación con *cooldown* (2 minutos) y respuestas uniformes que no revelan si una cuenta existe, ya está verificada, o está en cooldown

### 🔜 Próximas fases
- **Fase 3**: recuperación de contraseña
- **Fase 4**: segundo factor de autenticación (TOTP + backup codes)
- **Fase 5**: rate limiting, bloqueo de cuenta, logging de eventos de seguridad, panel de sesiones activas
- **Fase 6** (opcional): magic links, WebAuthn/Passkeys, Row Level Security en PostgreSQL

## Principios de seguridad aplicados

A lo largo del proyecto se han aplicado de forma consistente varios principios, documentados aquí para referencia rápida:

- **Nunca contraseñas en texto plano ni cifradas de forma reversible** — siempre hash con Argon2id
- **Nunca tokens (verificación, reset) en texto plano en base de datos** — siempre hasheados con SHA-256 antes de guardar
- **Mensajes de error genéricos** en login, registro y reenvío de verificación, para evitar enumeración de usuarios
- **Comparaciones en tiempo constante** (`CryptographicOperations.FixedTimeEquals`) al verificar hashes, para mitigar ataques de temporización
- **Fail open** ante fallos de servicios externos no críticos (Have I Been Pwned): la disponibilidad del registro no depende de un tercero
- **Defensa en profundidad**: validaciones duplicadas en cliente (UX) y servidor (fuente de verdad real), nunca solo en cliente
- **Separación de capas**: la lógica de negocio no conoce detalles de HTTP, EF Core o proveedores externos concretos

## Requisitos previos

- .NET 8 SDK
- Docker Desktop (para PostgreSQL y Mailhog)
- Visual Studio 2022 (o el IDE de tu preferencia)

## Puesta en marcha

1. Levantar los servicios de infraestructura:
   ```bash
   docker compose up -d
   ```
   Esto levanta PostgreSQL (puerto `5432`) y Mailhog (SMTP en `1025`, interfaz web en `8025`).

2. Restaurar dependencias y aplicar migraciones:
   ```bash
   dotnet restore
   ```
   Desde la Consola del Administrador de Paquetes en Visual Studio (proyecto predeterminado: `LoginLab.Infrastructure`):
   ```powershell
   Update-Database -Project LoginLab.Infrastructure -StartupProject LoginLab.Web
   ```

3. Ejecutar el proyecto web (`LoginLab.Web`) con F5 o:
   ```bash
   dotnet run --project LoginLab.Web
   ```

4. Abrir `http://localhost:8025` para ver los correos capturados por Mailhog durante las pruebas (registro, verificación de email, etc.)

## Estructura de la base de datos (hasta la fecha)

| Tabla | Propósito |
|---|---|
| `users` | Usuarios registrados, credenciales hasheadas, estado de verificación |
| `sessions` | Sesiones activas/revocadas, vinculadas a la cookie de autenticación |
| `email_verification_tokens` | Tokens de un solo uso para verificar email, almacenados hasheados |

## Notas y decisiones de diseño relevantes

- **Cookies vs JWT**: se optó por cookies de servidor con validación contra base de datos en cada petición, priorizando la capacidad de revocación instantánea sobre el rendimiento de un esquema *stateless*. El coste (una consulta a BD por petición autenticada) es asumible a la escala de este laboratorio.
- **Sin complejidad de contraseña forzada**: decisión deliberada y alineada con las guías actuales de NIST, priorizando longitud y comprobación contra filtraciones conocidas por encima de reglas como "mínimo una mayúscula, un símbolo, etc."
- **Result Pattern en lugar de excepciones para flujos de negocio**: operaciones como registro o login devuelven objetos de resultado (`AuthResult`, `LoginResult`) en lugar de lanzar excepciones para casos esperables (credenciales incorrectas, email duplicado). Las excepciones se reservan para errores realmente excepcionales.

## Licencia

Proyecto personal con fines educativos.
