# Bear Necessities workspace

Two projects live here: `BearNecessities/` (.NET 8 backend) and `bear-necessities-web/` (Angular frontend). Each has its own CLAUDE.md with code conventions.

The app is called **Bear Necessities** everywhere: user-facing text says "Bear Necessities", code identifiers use `BearNecessities` (namespaces, projects, `BearNecessitiesDbContext`), folders and packages use `bear-necessities-web`. Earlier names were Nomadica (code) and HippieLand (UI); only these leftovers keep the old name on purpose:
- `nomadica-media` — the real Cloudflare R2 bucket name; renaming it means creating a new bucket.
- `demo@nomadica.app` — the demo account in the database and `PlaceImportService` (owner of imported places). Rename it in code and DB together.
- The SQL Server database `Nomadica` and Docker containers `nomadica-sql` / `nomadica-seq` (and their volumes).
- The old session files `HippieLand-HANDOFF.md` / `HippieLand-session-transcript.json`.

Full history of the project (architecture, how to run, gotchas, pending work as of July 2026) is in `HippieLand-HANDOFF.md` in the Google Drive folder "Nomadica".

Restored here on 2026-10-27 from that Drive backup. Places come from OpenStreetMap: `dotnet run --project BearNecessities.API -- import-osm --iso PT --country Portugal`, adding `--by-region` for big countries (GR, ES, IN, ID) so each admin region is its own query.

Secrets (DB connection string, JWT key, R2 keys) live in `dotnet user-secrets` for BearNecessities.API, not in appsettings*.json — keep it that way. They still need rotating before deploy (the old values sat in plain config and on Drive). Run `dotnet user-secrets list` in `BearNecessities/BearNecessities.API` to see which keys are set.
