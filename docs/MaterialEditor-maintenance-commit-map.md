# Material Editor maintenance commit map

Baseline: `b82cf9e672e237dda57c95cd56238ef27954b6bf`.
Branch: `material-editor-maintenance-phase1-2`.

The existing work was split without adding further features. Commit numbers below
describe this delivery, not the numbering of the earlier long-term architecture plan.

| Order | Commit | Scope |
| --- | --- | --- |
| 1 | `e2441858` | Structured operation results, owner-scoped request queues, cancellation and lifecycle adapters |
| 2 | `a145c153` | Shared Texture2D/Cubemap transactions, persistence/load/restore decomposition and serialization helpers |
| 3 | `65d4f7de` | PluginBase configuration, nested shader models/parsing and asset helper separation |
| 4 | `3d085eab` | Property builders/binding, dropdown/style components and virtual-list helper separation |
| 5 | `b9cc3e0c` | Exact list-backed property queries shared through typed Chara/Studio adapters |

The first slice includes the audit corrections for reentrant watcher coalescing
and legitimate caller-provided subtree scope. The query slice adds no persistent
index or cache. The virtual-list slice does not tune cache limits or visible ranges.
No synchronous save/load callback was converted to asynchronous execution.

## Verification at split completion

- Each of the five source slices passed a KKS build before its commit.
- The final source state passed full rebuilds for API, KK, KKS, EC, AI, HS2 and PH.
  Builds retained warnings; zero errors does not mean warning-free.
- The separate managed-code smoke script passed 13 focused checks using Unity
  stand-ins. It does not exercise Unity runtime behavior.
- The existing MaterialEditor.MetadataTests runner passed.
- Final source contents match the pre-split backup, excluding line-ending and
  trailing end-of-file whitespace normalization.
- Full in-game compatibility and performance verification remain deferred.
- No binaries were installed and no commits were pushed during this split.

The earlier [phase 1/2 implementation note](MaterialEditor-architecture-maintenance-phase1-2.md)
is retained as a historical report. Its line counts and 12-check result describe
that earlier checkpoint, not the final five-slice state. Future scheduling work
mentioned there is outside this delivery.

Follow [the maintenance guidelines](MaterialEditor-maintenance-guidelines.md)
for subsequent work. Documentation is committed separately from the five source slices.
