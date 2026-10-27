# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build, Run & Test Commands

```bash
# Build entire solution
dotnet build Nomadica.sln

# Run the API
dotnet run --project Nomadica.API

# EF Core migrations (always specify both -p and -s)
dotnet ef migrations add <Name> -p Nomadica.Infrastructure -s Nomadica.API
dotnet ef database update           -p Nomadica.Infrastructure -s Nomadica.API
```

No test project exists yet. When one is added, run a single test with:
```bash
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"
```

Swagger is available at `https://localhost:<port>/swagger` after `dotnet run`.

---

## Architecture

**Clean Architecture** across four projects — dependency flow is strictly inward:

| Project | Role |
|---|---|
| `Nomadica.Domain` | Entities, value objects, domain interfaces. Zero framework dependencies. |
| `Nomadica.Application` | Use-case handlers, DTOs, mapping (not yet implemented). |
| `Nomadica.Infrastructure` | EF Core 8, repositories, Unit of Work, DI wiring. |
| `Nomadica.API` | ASP.NET Core 8 entry point; calls `AddInfrastructure()` only. |

### Data flow (request path)

```
HTTP → Controller → Application use case → IUnitOfWork/IRepository → EF Core → SQL Server
```

### Key patterns

**Entities** are instantiated only through static `Create()` factory methods. All setters are private; state changes go through domain methods (`Place.Approve(userId)`, `Event.Cancel()`).

**Value-object IDs** (`UserId`, `PlaceId`, `EventId`, `CategoryId`, `Coordinates`, `GeoViewport`) are C# records wrapping `Guid`. EF Core converts them to/from `Guid` via `HasConversion()` in each entity configuration. This prevents ID type confusion at compile time.

**Repository + Unit of Work** — `IUnitOfWork` composes typed repositories and exposes `SaveChangesAsync()`. Never resolve `NomadiaDbContext` directly outside of Infrastructure.

**Soft delete** — entities that extend `AuditableEntity<TId>` have `DeletedAt` / `IsDeleted`. A global `HasQueryFilter` on `Place` automatically excludes deleted records; other auditable entities should follow the same pattern.

**Timestamps** — `NomadiaDbContext.SaveChangesAsync()` sets `UpdatedAt = DateTime.UtcNow` for every modified entity. All timestamps are UTC.

**Geospatial** — `Coordinates` value object validates lat/lng ranges. `GeoViewport` represents a NE/SW bounding box. Entities store `H3IndexRes5` and `H3IndexRes8` for geo-partitioned queries; these must be populated by the infrastructure layer before saving.

**DI registration** lives entirely in `Nomadica.Infrastructure/DependencyInjection.cs` (`AddInfrastructure()`). All repositories and `IUnitOfWork` are scoped.

---

## Non-Obvious Conventions

- **EF config**: one `IEntityTypeConfiguration<T>` class per entity in `Nomadica.Infrastructure/Configurations/`. Use `PropertyAccessMode.Field` for collection backing fields. Always declare `HasIndex` on columns used in `WHERE` clauses (status, OwnerId, H3 hashes, lat/lng pairs).

- **Collection backing fields**: entities expose collections via `IReadOnlyList<T>` properties backed by private `List<T>` fields. Never replace the list — mutate it through entity methods.

- **Polymorphic relationships** (`SavedItem`, `Conversation`, `Media`, `ListingChangeLog`) use `TargetType`/`ContextType` enum + `TargetId`/`ContextId` Guid pairs instead of foreign keys, because the target type is not known at schema design time. There are no EF navigation properties on these; resolve manually in repositories.

- **Review reveal mechanics** — `Review` has a `RevealedAt` field. Both author and counterparty reviews are hidden until a synchronisation point is reached; application logic drives this, not EF global filters.

- **No ApplicationUser / Identity** — `User` is a plain domain entity. Auth provider integration is handled via `UserSocialLogin` (Provider + ProviderId). ASP.NET Core Identity is not used.

- **`ListingChangeLog`** stores `OldData`/`NewData` as JSON strings — serialise with `System.Text.Json`, not Newtonsoft.

- **Recurring events** use `RRule` string (iCalendar format) on `Event`; expansions are materialised into `EventOccurrence` rows by a scheduled job (not yet implemented).

---

## Code Quality Standards

These rules are always on — apply them to every file touched.

### No duplication
Grep before writing anything new. Reuse and consolidate near-duplicates. If a method does the same thing in two places, extract it.

### SOLID
- **SRP** — one responsibility per class and method. A method that queries AND maps AND sends email is three methods.
- **OCP** — extend via abstraction (new implementation of an interface), never by modifying existing logic behind a conditional.
- **LSP** — no `NotImplementedException` or `throw new Exception("not supported")` overrides. If a subtype can't honour the contract, it is the wrong subtype.
- **ISP** — split fat interfaces. If a consumer uses only two of eight members, define a smaller interface.
- **DIP** — inject dependencies via constructor. Never `new` up a service, repository, or `HttpClient` inside a class body.

### Minimal external calls
Never call the same endpoint or run the same query twice in one request. No awaits inside loops. Batch or cache instead.

### Clear naming
- Methods: verb + noun (`GetActiveEvents`, `SubmitReview`).
- Booleans: `is`/`has`/`can`/`should` prefix (`isDeleted`, `canRsvp`).
- No abbreviations — `mgr`, `svc`, `val`, `obj`, `tmp`, `res` are banned.
- No single-letter variables outside `for` loop indices.

### Comments
Write only WHY (hidden constraint, non-obvious invariant, known bug workaround). One line max. Delete comments that restate the code. Delete all commented-out code blocks.

### Efficiency
- Guard clauses instead of nested `if/else`.
- `async`/`await` throughout — never `.Result` or `.Wait()`.
- Remove dead code. Extract magic strings to `const` or configuration.
