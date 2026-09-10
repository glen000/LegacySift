# LegacySift safety model

LegacySift is designed around one fixed direction:

- **OLD folder**: may be changed during an explicit cleanup.
- **CURRENT folder**: protected comparison reference; never cleaned.

This distinction is intentionally hard-coded into the product model rather than exposed as a generic left/right sync choice.

## Core invariants

1. **Comparison is read-only.** The analysis phase enumerates files and reads metadata/content. It does not move, rename or delete user files.
2. **Cleanup candidates originate only from the OLD tree.** Every cleanup item is created from an exact-duplicate record whose source belongs to the old-folder scan.
3. **Path containment is checked again at cleanup time.** Before a relative source path is built, `PathSafety.GetRelativePath` confirms that the file is still below the OLD root.
4. **The CURRENT copy must still exist and still match.** Immediately before cleanup, LegacySift re-checks file sizes and recalculates SHA-256 for both files. Any mismatch means the OLD file is left untouched.
5. **Safety-folder moves are verified after the move.** The moved file is hashed again. If verification fails, LegacySift attempts to move it back to its original location and reports an error.
6. **Restore never overwrites.** If the original path already contains a file, restore reports a conflict and leaves both files untouched.
7. **Unsafe folder relationships are blocked.** Identical roots, nested roots, drive roots, and major Windows system folders as the OLD root are rejected.
8. **Reparse points are skipped.** Junctions, symbolic links and other reparse points are not traversed during recursive scanning.
9. **No privilege elevation.** The application manifest requests `asInvoker`. A protected file/folder that the current Windows user cannot access is reported rather than triggering elevation.
10. **No network path is required by the product.** LegacySift does not contain upload, telemetry, update-download, or cloud-service logic.

## Threat model

LegacySift is primarily intended to reduce accidental data loss during manual migrations and backup cleanup. It is not intended to defend against a malicious local administrator, compromised operating system, malicious filesystem driver, hardware failure during writes, or cryptographic attacks against the host machine.

## Recommended testing behavior

Until the first stable release, always use the **Safety folder** cleanup method on real data and keep an independent backup while testing.
