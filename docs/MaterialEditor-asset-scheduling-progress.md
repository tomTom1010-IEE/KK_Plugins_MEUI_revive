# Asset scheduling maintenance

Baseline: `2c853032`. Public synchronous entry points and saved-data schemas remain
unchanged. Each module is compiled and committed before dependent work begins.

## Background Texture2D reads

Built-in deferred imports now read on a worker and apply prepared bytes from the
controller queue's main-thread pump. Target identity is checked again before apply.
The synchronous file-path API intentionally retains synchronous behavior.

One shared reader admission slot includes the ready buffer until consumption;
up to 32 waiting readers hold paths, not byte arrays. Cancellation of an active
read retains admission until the worker exits. Reads check cancellation between
64 KiB chunks. Transient IO failures get at most three attempts; decoding is not
retried. No new image quality or file-size policy was introduced.

Unity decode, animation parsing, texture registration and upload remain on the main
thread. Background reading alone does not eliminate those stalls. The reader is
internal to each compiled plugin assembly, as are the existing queues/cache.

Verification: KKS build and managed scheduling checks passed (payload, ready-buffer
admission, ordering, application thread, cancellation and read-error results).
These checks use stand-ins and do not establish in-game behavior.

## Cooperative Cubemap import budget

Interactive acquisition now checks a 2 ms internal time budget between scanline
units, capped at 16 rows per frame. At least one unit runs to guarantee progress.
The existing synchronous acquisition path is unchanged. PNG decode, face uploads
and final Apply remain indivisible and can exceed the budget. Managed checks cover
the unit cap and forward progress, not real Unity frame-time guarantees.

## Staged Cubemap export

Interactive export owns a bounded queue, conversion reservation, face snapshot,
projection buffer and writer. It captures all six faces in the same frame so
in-place changes by external plugins cannot produce a mixed-generation panorama.
The existing reusable GPU readback resources and color handling are retained.
This deliberately does not claim to eliminate the synchronous six-face readback.

Projection uses the shared cooperative budget. Unity texture creation/upload and
PNG encoding run in a later main-thread stage and remain indivisible. File bytes
are written in worker chunks to a unique sibling temporary file; only a validated
main-thread completion publishes it with same-directory move/replace. A failed or
cancelled write does not truncate an existing destination. Memory admission remains
owned until the writer has exited, including cancellation. Normal Texture2D export
and original-byte export retain their existing synchronous paths in this module.

The synchronous Cubemap conversion API drives the same operation to completion.
No projection formula, sampling, panorama dimensions or source-size limit changed.
KKS build and managed write checks passed; readable/non-readable export direction,
color and actual frame times still require Unity/game validation.
