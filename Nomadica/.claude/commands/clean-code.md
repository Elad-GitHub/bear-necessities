# /clean-code

Perform a focused code-quality review and fix pass on changed files. Fixes are applied directly — not just reported.

## Scope

If arguments are provided, target only those files. Otherwise, derive scope automatically:

```bash
# Changed files
git diff --name-only HEAD

# Untracked .cs files
git ls-files --others --exclude-standard -- "*.cs"
```

Ignore files outside that set. Do not refactor callers or related files unless a rename makes it unavoidable — and if so, fix all call sites.

---

## Pass order (run in sequence, do not skip)

### 1. No duplication
Before adding anything new, `Grep` for the concept across the solution. If a near-duplicate exists in another file, consolidate them into a shared location (extension method, base class, helper — whichever fits the existing pattern). Apply the same check to magic strings: if a literal appears more than once, extract it to a `const` or configuration key.

### 2. SOLID
- **SRP** — if a method does more than one thing (queries + maps + sends, validates + persists + notifies), split it. One responsibility per class and per method.
- **DIP** — replace every `new SomeService(...)` or `new SomeRepository(...)` inside a class body with a constructor-injected dependency. Register it in `AddInfrastructure()` or the appropriate DI extension if not already there.
- **ISP** — if an interface has members that some implementors leave unimplemented (or `throw new NotImplementedException()`), split the interface. Remove the `NotImplementedException` overrides after splitting.
- **OCP / LSP** — flag (with a `// TODO:` comment) any conditional logic that branches on concrete types (`is SomeConcreteClass`) or any override that weakens the base contract. Do not silently ignore these.

### 3. Minimal API / query calls
- Identify every `await` inside a `foreach`, `for`, or LINQ expression. Batch the calls or restructure to fetch all data before the loop.
- Identify duplicated awaits of the same expression within one method (`await repo.GetAsync(id)` called twice). Capture the result in a local variable on first use.
- Identify N+1 patterns in EF Core (lazy-loaded navigation accessed inside a loop). Add `.Include()` or a batch query instead.

### 4. Clear naming
Rename any identifier that:
- Uses a banned abbreviation: `mgr`, `svc`, `val`, `obj`, `tmp`, `res`
- Uses a vague verb alone: `Process`, `Handle`, `Do`, `Run` with no noun
- Is a single letter outside a `for` loop index
- Is a boolean without an `is`/`has`/`can`/`should` prefix

Fix all call sites within the scoped files. If a public API surface changes, note it in the summary.

### 5. Comments
- Delete every comment that restates what the code already says.
- Delete every commented-out code block.
- Trim multi-line `// ...` comments to one line max.
- Keep only comments that explain WHY: a non-obvious constraint, a known external bug workaround, a deliberate trade-off.

### 6. Efficiency
- Replace nested `if/else` chains with guard clauses (early returns / early throws).
- Replace any `.Result`, `.Wait()`, or `Task.Run(...).Result` with `await`.
- Remove dead code: unreachable branches, unused private methods, unused `using` directives.
- Extract inline magic strings (URLs, status strings, error messages used more than once) to `const` fields or configuration.

---

## Rules

- **Do not change behaviour.** If a fix would alter observable output, skip it and note it in the summary instead.
- **Do not add abstractions for hypothetical future use.** Only consolidate duplication that already exists.
- **Do not touch out-of-scope files** — except to fix renamed call sites.
- **Apply fixes directly** — edit the files, do not just report them.

---

## Output

End with a concise summary — one bullet per file:

```
- Nomadica.Infrastructure/Repositories/PlaceRepository.cs — extracted duplicated viewport-to-SQL translation (Rule 1); replaced `new UnitOfWork()` with injected dependency (Rule 2).
- Nomadica.Domain/Entities/Event.cs — renamed `HandleRsvp` → `ProcessRsvpRequest` is too vague; renamed to `RecordRsvp` (Rule 4); removed 3 commented-out blocks (Rule 5).
```

State which rule number was addressed for each file. If a file had no issues, omit it from the list.
