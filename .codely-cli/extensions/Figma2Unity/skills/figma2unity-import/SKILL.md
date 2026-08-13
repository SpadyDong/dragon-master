---
name: figma2unity-import
description: Import Figma designs into Unity or Tuanjie (团结引擎) using the cn.tuanjie.figma2unity package. Use when the user asks to import/bring a Figma design, mockup, or design draft (设计稿) into Unity/Tuanjie, run Figma2Unity, sync a Figma file into UGUI, or mentions a Figma file key / Personal Access Token for importing. Verifies the package is installed, that the Figma File Key and Personal Access Token are set, then triggers the Figma2Unity import.
---

# Figma2Unity Import

Execute the scripts in `scripts/` in order via `execute_csharp_script` (pass the absolute
`script_path`, do NOT paste the code inline and do NOT `read_file` first). Parse the
`[F2U-CHECK]` log lines from preflight (steps 1–2) to decide what to do next.

**Skill integrity rule:** The files under this skill directory are read-only templates.
NEVER edit, overwrite, or fill in placeholders inside `scripts/` (e.g. `set_credentials.cs`).
When a script needs runtime values, write the filled copy to a throwaway path OUTSIDE this
skill directory (use the system temp dir, e.g. `/tmp/f2u_set_credentials_<timestamp>.cs`),
run that copy, then delete it immediately after it finishes — even on failure. No task-specific
data (File Keys, PATs, URLs) may ever be persisted inside this skill.

**Hard rule:** This skill ONLY triggers the import. It does NOT verify, wait for, or report
on the import result. After step 3 fires the import, you are DONE. Do not call
`unity_console`, do not `wait_for_*`, do not poll, and do not check whether the import
succeeded — that is out of scope.

Facts: package id `cn.tuanjie.figma2unity`; credentials live in Unity `EditorPrefs`
(`Figma2Unity.FileKey`, `Figma2Unity.{ProductName}.PAT`), not in a config asset; Import is a
button on the `Figma2Unity/Open Main Window` panel whose handler is the private
`Figma2Unity.Editor.F2UMainWindow.ImportAsync()`. The import is triggered by calling that
function on a hidden window instance — the panel is NOT shown.

## 1. Preflight

Run `scripts/preflight.cs`. It logs:

- `[F2U-CHECK] package=<bool>` — if `false`, STOP: tell the user to install
  `cn.tuanjie.figma2unity` (Package Manager / add to `Packages/manifest.json`).
- `[F2U-CHECK] fileKey=<bool>` — if `false`, ask the user for their Figma File Key or URL.
- `[F2U-CHECK] pat=<bool>` — if `false`, ask the user for their Personal Access Token.
- `[F2U-CHECK] ready=<bool>` — `true` means proceed to step 3.

## 2. Set missing credentials

Only if step 1 reported a missing value. Do NOT modify `scripts/set_credentials.cs` — treat it
as a read-only template. Instead:

1. Read `scripts/set_credentials.cs` to get the template text.
2. Replace `__FILE_KEY__` and/or `__PAT__` with the user-supplied values (use the literal
   `__SKIP__` for a value that is already set).
3. Write the filled result to a temp file OUTSIDE this skill directory, e.g.
   `/tmp/f2u_set_credentials_<timestamp>.cs`.
4. Run that temp file via `execute_csharp_script`.
5. Delete the temp file immediately afterward (always, even if the run failed).

Never invent values; never echo the PAT back. Then re-run `scripts/preflight.cs` and require
`ready=true`.

## 3. Import

Save the scene: `unity_scene { "action": "ensure_scene_saved" }`. Then run
`scripts/run_import.cs`. It invokes `ImportAsync()` on a hidden window instance, so the
panel never pops up.

This is the final step. As soon as `run_import.cs` has been executed, the task is complete —
tell the user the import was triggered and STOP. Do NOT read the console output of
`run_import.cs`, do NOT call `unity_console`, do NOT wait for or verify the import result.
