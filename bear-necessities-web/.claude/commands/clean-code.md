# clean-code

Review and fix code quality issues in changed files.

Scope: files changed since last commit (`git diff --name-only HEAD`) plus untracked `.ts` files, or specific files if passed as arguments.

Enforces these rules in order, fixing issues directly:

## 1. No Duplication
Grep for patterns before adding anything new. Consolidate near-duplicates across files.
- Search for similar method implementations, repeated API calls, duplicated logic
- Extract to shared utilities, base classes, or services
- Rename to avoid conflicts, update call sites

## 2. SOLID Principles
Fix violations in this order:
- **SRP**: Each class has one reason to change. Split god objects; extract cross-cutting logic to services/interceptors.
- **OCP**: Don't modify existing code to extend—use inheritance, composition, interceptors, or dependency injection.
- **LSP**: No `NotImplementedException` or stub methods. If a method doesn't apply, split the interface or remove the contract.
- **ISP**: Don't force classes to implement unused methods. Keep interfaces focused.
- **DIP**: Inject services via constructor; never use `new MyService()` inside component logic (only `new` for plain objects).

## 3. Minimal API Calls
Eliminate performance anti-patterns:
- **No loops-with-calls**: Never call an endpoint inside a `for`/`forEach` loop. Batch data in one request.
- **No N+1**: Load all needed data in one query, not per-item (e.g., fetch all place details at once, not per marker).
- **No repeated awaits**: If the same observable/promise is subscribed multiple times, cache it in a signal or variable.
- Use `shareReplay()` for hot observables that multiple subscribers need.

## 4. Clear Naming
Rename anything that hides intent:
- **Methods**: Use `verb + noun` (e.g., `loadPlaces()`, `addMarker()`, not `fetch()`, `handle()`).
- **Booleans**: Prefix with `is/has/can/should` (e.g., `isLoading`, `hasError`, `canNavigate`).
- **Banned words**: `mgr`, `svc`, `val`, `obj`, `tmp`, `res`, `data`, `item`, `thing`, `result`, `value` — spell them out.
- **Single-letter variables**: Allowed only for loop indices (`i`, `j`) and destructuring. Not `x`, `e`, `r`.
- **Process/Handle/Do/Run alone**: Too vague. Use `loadData()`, `fetchUser()`, `calculateTotal()`.

## 5. Comments
Delete noise, keep only WHY:
- **Remove comments that restate code**: `// set isLoading to false` → delete.
- **Delete commented-out code blocks**: Git has history. Don't clutter.
- **Keep WHY comments**: Non-obvious constraints, trade-offs, workarounds (one line max).
- Example keep: `// Must use .slice() to trigger change detection, not .push()`.

## 6. Efficiency
Guard clauses, async/await, dead code removal:
- **Guard clauses**: `if (!condition) return;` instead of nested if/else.
- **async/await throughout**: No `.Result`, `.Subscribe()` in component methods, no callback chains.
- **Remove dead code**: Unused imports, unreachable branches, stub methods, unused variables.
- **Magic strings**: Extract to `const` or `environment` (e.g., `apiUrl`, `TOKEN_KEY`).
- **O(n²) loops**: Look for nested loops calling methods; batch or use Maps/Sets.

## Rules
- **Don't change behavior**: Only refactor to meet the rules above.
- **Don't refactor out-of-scope files**: Stick to changed files unless a nearby file's change requires cleanup.
- **Don't add premature abstractions**: Three duplicates justify extraction; one or two don't.
- **Fix callers when renaming**: Update all call sites to use the new name.

## Output Format
End with a one-bullet-per-file summary of what changed and which principle it addressed.

Example:
```
- **map.component.ts**: Extracted duplicate place-filter logic into shared utility (No Duplication); renamed `f()` to `formatPlaceName()` (Clear Naming)
- **api.service.ts**: Removed cached value assignment, use computed signal instead (DIP, Efficiency)
```

---

## Usage
Run `/clean-code` to review all changed files, or `/clean-code src/app/core/services/auth.service.ts` for specific files.