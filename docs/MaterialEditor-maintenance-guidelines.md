# Material Editor maintenance guidelines

## Purpose and compatibility contract

Keep Material Editor maintainable through small, reviewable structural changes.
Refactoring must preserve existing user-visible behavior unless a separately
identified bug fix or feature explicitly changes it.

- Preserve ExtendedSaveData plugin identifiers, versions, dictionary keys,
  absent/null/empty semantics, MessagePack keys, DTO field types and defaults.
- Preserve public signatures, overloads, default arguments, nested type names and
  extension entry points. Moving a nested type to a namespace is an API change.
- Preserve load/application order, coordinate and object scope, migration rules,
  duplicate-record handling and animation state. Do not silently normalize old data.
- Keep serialized DTOs separate from runtime queues, leases, caches and UI state.
- Any intentional compatibility change requires its own proposal and commit;
  it must not be hidden inside a structural cleanup.

## Commit discipline

1. Record the baseline, branch, working-tree state and scope before editing.
   Preserve unrelated user changes and work in an isolated worktree when appropriate.
2. Plan dependency-ordered slices before implementation. Finish and commit one
   coherent slice before starting another; do not accumulate several phases uncommitted.
3. A slice includes its shared-project compile entries and all required adapters.
   It must build without relying on unstaged or untracked implementation files.
4. Separate behavior fixes, structural moves, query changes and broad formatting.
   If a prerequisite fix is necessary, identify it explicitly in the commit notes.
5. Stage explicit paths or hunks. Inspect the staged diff and whitespace checks;
   never use a blanket add to conceal cross-module changes.
6. Compile the affected representative target for each slice. For shared changes,
   rebuild API, KK, KKS, EC, AI, HS2 and PH before handoff/publication.
7. State what was verified, what remains untested and whether anything was installed
   or pushed. A local commit does not imply permission to push or install binaries.
8. Do not rewrite published history without agreement. If earlier work was not
   sliced, back it up, reconstruct dependency-complete commits and compare the
   final tree against the backup before continuing development.

Use repository line-ending conventions: these C# and shared-project files use
CRLF in the working tree. Preserve existing BOM/encoding choices, avoid unrelated
newline changes and inspect the effective diff rather than relying on editor defaults.

## Responsibility boundaries

| Area | Owns | Must not become |
| --- | --- | --- |
| Operation queue/workflow | Acceptance, ordering, cancellation, completion, owner lifetime | Persistence storage or a second transaction implementation |
| Shared transactions | Prepare, apply, commit, rollback and resource ownership transfer | Game-specific object discovery or UI layout |
| Chara/Studio persistence adapters | Destination resolution, location fields, serialization orchestration | Duplicated transaction/state machines |
| PluginBase | Stable entry points and initialization orchestration | A parser, configuration binder, exporter and UI manager in one file |
| Row builders/binding | Row construction and shared target metadata | Persistence or asset import execution |
| Dropdown/style components | Appearance, geometry, listeners and lifecycle in explicit owners | Implicit main-window resizing or repeated layout policies |
| Virtual-list components | Visible range, view pool, binding lifecycle and cache policy | Hidden ownership of model data |
| Record query adapters | Exact matching over authoritative lists | A second mutable database or serialized index |

Partial classes are organizational tools, not proof of abstraction. Prefer one
coherent responsibility per file. Review growing files around 400-500 lines and
methods around 60-80 lines as inspection signals, not mandatory size limits.
Split by ownership and behavior, not arbitrary line ranges. Avoid replacing one
large method with many wrappers that still duplicate the same state machine.

Share genuinely identical Chara/Studio steps through internal composition or typed
adapters. Keep destination-specific differences visible. Do not change persistence
DTOs or publish new interfaces merely to remove a few repeated lines.

## Operations and resource lifetime

- Every asynchronous operation has an owner, a bounded admission policy, an explicit
  result and exactly-once completion. Cancellation, supersession and unverified
  legacy execution are not successful application.
- Revalidate destination identity before applying deferred work. Preserve legal
  caller-provided subtree scope; do not equate it with the slot root unnecessarily.
- Reload, replacement, disposal and deactivation must invalidate owned work.
  Late callbacks must not mutate a new target or revive disposed workflows.
- Watcher coalescing must not reorder explicit user requests. Treat callbacks as
  reentrant: they may dispose an owner or enqueue more work.
- Specify who owns each lease, temporary texture, snapshot and cancellation handle,
  including ownership transfer and exception paths. Restore runtime state before
  releasing resources required by rollback.
- Build detached candidates where appropriate. Publish persistence changes only
  after successful application; preserve existing DTO identity when consumers rely on it.
- Log actionable failures. Do not swallow exceptions and report success, or let
  one malformed record prevent unrelated records from loading without a reason.

## UI and query invariants

- Pure UI refactoring must preserve geometry, spacing, visibility, expansion state,
  focus, scrolling anchors, tooltips and input behavior. Layout fixes belong in
  separate changes with an explicit visual intent.
- Keep side-panel state independent of center-panel sizing. Do not reintroduce
  automatic expansion, empty collapsed columns or overlapping title-bar controls.
- Recycled views must release old listeners and bindings before reuse. Pool policy
  must not retain disposed owners or stale row targets.
- Preserve exact property/material matching, location isolation, first-match order,
  additional removal predicates and remove-all behavior for duplicate records.
- Lists remain authoritative. External DTO edits, rename, reload and copy must be
  visible immediately. Add an index only with measured benefit and a complete,
  documented invalidation strategy; do not deduplicate records as an optimization.

## Blocking work and performance claims

Cubemap/HDR work has substantial inherent allocation, conversion and upload costs.
Scheduling can improve responsiveness without reducing total work or final memory.
Do not describe moving work to a coroutine as making that work non-blocking.

Keep Unity object access and uploads on the required thread. Move eligible CPU/I/O
work only across explicit ownership and cancellation boundaries. Record remaining
synchronous sections and account for concurrent buffers and peak memory.

Require measurements before changing indexing, cache limits or virtualization:
record target game, data size, UI scale, workload, elapsed/frame time and allocations.
A synthetic benchmark is not in-game evidence. Keep scheduling changes separate
from persistence reorganization; asynchronous load completion is a behavior change.

## Verification and handoff

During structural maintenance, prioritize code boundaries and compatibility review,
with compile checks and focused existing checks after each relevant slice. The full
in-game regression stage may follow the initial maintenance, but must remain an
explicit outstanding release gate rather than being replaced by successful builds.

Handoff notes must list commit boundaries, touched contracts, known remaining
synchronous work, build/test results and deferred checks. Later runtime coverage
must include old cards/coordinates/scenes, import/copy/reset, animation, failed and
cancelled imports, Maker/Studio UI and target replacement. Never claim runtime
compatibility was established solely by compilation or unchanged DTO source.
