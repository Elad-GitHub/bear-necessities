# HippieLand workspace

Two projects live here: `Nomadica/` (.NET 8 backend) and `nomadica-web/` (Angular frontend). Each has its own CLAUDE.md with code conventions.

User-visible app name is **HippieLand**; everything in code is **Nomadica**.

Full project context (architecture, how to run, gotchas, pending work) is in `HippieLand-HANDOFF.md` in the Google Drive folder "Nomadica". Read it before starting work.

Restored here on 2026-10-27 from that Drive backup. The old database was not backed up: recreate it with EF migrations, then re-import places with `dotnet run --project Nomadica.API -- import-osm --iso GR --country Greece` (and PT, MX, TH, CR).

Config files contain live secrets (R2 keys, JWT key, sa password). Never push them to a public repo; rotate before deploying.
