# Material Editor architecture maintenance — phases 1 and 2

Historical checkpoint: see the [commit map](MaterialEditor-maintenance-commit-map.md)
for the final five-slice delivery and updated verification results.

## Baseline and scope

Implemented on `material-editor-maintenance-phase1-2`, based on upstream
`b82cf9e672e237dda57c95cd56238ef27954b6bf` (merged PR #403 plus follow-up maintenance).
The earlier development checkout is untouched.

This change is structural maintenance of asset operations and persistence.
It does not change the tooltip format, UI layout, serialized DTO fields, or public
Material Editor method signatures. Internal repository completion methods now
return a cancellation handle and a structured result.

## Phase 1: operation ownership

- `MaterialEditResult` distinguishes Succeeded, Failed, Cancelled, Superseded,
  and Unverified, with a stage and diagnostic. Legacy void imports are explicitly
  Unverified rather than assumed successful.
- `MaterialEditRequestQueue` is a main-thread, owner-scoped FIFO, bounded at
  32 outstanding operations. Each pump starts at most one request. User imports
  remain ordered; only pending watcher updates for the same target/property/path
  coalesce. Capacity exhaustion produces an explicit failure.
- Runtime target identity captures the root, formatted material name, material
  instance, property and shader. Controller adapters additionally capture the
  coordinate/slot/object type or Studio object ID and validate the destination
  before executing deferred work. No persistent index or new save-data key was added.
- Texture2D controllers no longer use a single replaceable pending-file slot.
  UI Texture2D and Cubemap imports share a workflow queue, so Cubemap background
  reads start only after acceptance into that queue.
- Cancellation handles address one repository request. Reset and shader changes
  invalidate queued operations for the affected material/property. Reload,
  coordinate changes, UI release, and runner deactivation cancel owned work.
- Each workflow instance is a one-shot lifetime token: after disposal, file-dialog
  callbacks, watcher callbacks and row refreshes cannot reuse it.
- The process-wide watcher retains a specific owner/instance. Event processing
  is marshalled to the main thread and notifications coalesce before imports.
  An old watcher cannot dispose its replacement.
- Cubemap runners are cancelled by their workflow and on deactivation/destruction.
  Completion is exactly once, including cancellation and late callbacks.

## Phase 2: transaction and persistence boundaries

### Shared operations

- `MaterialCubemapImportTransaction`: acquisition, byte/lease storage, runtime
  snapshot, detached candidate, apply, commit, and rollback. Chara/Studio adapters
  supply only their record lookup, location fields and storage access.
- `MaterialTextureImportTransaction`: the shared prepare/apply/commit boundary
  for Texture2D and animated textures.
- `MaterialTextureImportCommit`: transfers texture/animation state while retaining
  an existing DTO's identity; restores the old ID, animation definition and
  animation-controller mapping if commit fails.
- `MaterialTextureSnapshot`: restores the actual material texture references
  before unsuccessful candidate resources are discarded.
- `MaterialCubemapOriginalState` remains the original-snapshot authority.
  Both persistence DTOs remain unchanged.

### Load and copy stages

`MaterialEditLoadContext` lives in the game persistence layer, not the standalone
API layer. It owns source data, texture-ID remapping and per-family/per-record
diagnostics. Byte-ID importing is shared; game-specific destination resolution
stays in the controllers.

Character/card and coordinate loads use shared property handlers with an explicit
coordinate policy. Selection/removal is separate from deserialization. Scene load
handlers are grouped into ordinary properties and textures; scene-copy handlers
accumulate into a batch that is published only after processing all destinations,
so newly copied records cannot become sources during the same event.

| Orchestration method | Before | After |
| --- | ---: | ---: |
| `LoadCharacterExtSaveData` | 330 | 75 |
| `LoadCoordinateExtSaveData` | 192 | 39 |
| `OnSceneLoad` | 270 | 85 |
| `OnObjectsCopied` | 162 | 52 |

Counts are physical lines including the method body, not complexity scores.

Preserved ordering:

- Character/card data: shader, renderer, projector, names, scalar/keyword/color/vector,
  Texture2D, Cubemap, material copies; runtime application remains in the existing
  `LoadData` path.
- Coordinate data retains its original family order and exclusion of projectors.
- Scene load/copy: material copies and names precede shaders, followed by property
  families and textures. Existing color-to-vector migration and keyword cleanup remain.
- Invalid individual records are logged and skipped instead of preventing later
  records/families from loading.

Removed the unused scene fields `AAAAAA` and `BBBBBB`, which only retained complete
serialized dictionaries after save/load and had no readers.

## Verification and remaining work

- API, KK, KKS, EC, AI, HS2 and PH compile successfully.
- A separate, lightweight managed-code smoke check passed 12 checks covering FIFO,
  watcher coalescing, capacity, cancellation isolation, late completions, reentrant
  disposal, failure recovery and texture/animation commit rollback.
  It uses Unity stand-ins and does **not** establish in-game behavior.
- Existing serialized model files were not changed.
- Full in-game/card/coordinate/scene regression testing remains deferred to the
  agreed later testing stage. Nothing has been installed or pushed by this change.
- Cubemap/HDR conversion and memory costs are not claimed to disappear. Further
  scheduling of synchronous Texture2D reads/decoding, hashing, Unity uploads and
  persistence processing belongs to phase 3. This change establishes ownership
  boundaries but does not promise that every Unity operation is non-blocking.
