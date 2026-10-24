# Jam-Build: progress

Unity 2D game for a game jam, built with friends. Unity **6000.5.4f1**, URP 2D, Input System, uGUI. KisuTH is the programmer on the team.
This file is the summary. Claude Code reads it at the start of every session and updates it after every change (see `CLAUDE.md`).

## Status at a glance (updated 2026-10-24)
- **Branch I work on:** `Coding-Rain`. Friend's branch: `SETTING-Scene-Gim` (scene/settings work). `main` is the shared version, merged through pull requests.
- **On `Coding-Rain` and pushed:** the Unity MCP plugin (`com.ivanmurzak.unity.mcp` 0.76.1, from OpenUPM) in `Packages/manifest.json`.
- **On `Coding-Rain`, merged locally, NOT pushed:** the 2 commits from `SETTING-Scene-Gim`.
- **Claude Code <-> Unity link:** the plugin window (Window > AI Game Developer) shows "Unity: Connected". The MCP entry (`.mcp.json`) was still "Not configured" when this file was written. Next step is Configure, then `claude` in the project folder and `/mcp`.
- Nothing has been tested in a build yet.

### What exists, by area (read from the files, not yet re-checked in play mode)
**Scripts (`Assets/Scripts`)**
- `PlayerMovement2D.cs`: side-scroller movement on a Rigidbody 2D: A/D walk, W jump, S crouch (changed 2026-10-24, built, untested). Needs Gravity Scale above 0 (e.g. 3), a Collider2D on the Player, and a solid (non-trigger) Collider2D on the ground. Old W/S up-down movement is gone.
- `PointAndClickPlayer.cs`: point-and-click movement.
- `Interactable.cs`: things the player can interact with.
- `ItemPickup.cs`, `Inventory.cs`, `InventoryUI.cs`: pick items up, keep them, show them.
- `DialogueUI.cs`: dialogue box.

**Added 2026-10-24 (built, untested): point-and-click core for the 2D horror/investigation idea**
- `Interactable2D.cs` + `ClickInteractor2D.cs`: click an object (Collider2D) to inspect/use it; distance check, optional required item (e.g. key).
- `ItemPickup.cs`, `Inventory.cs`, `InventoryUI.cs`, `DialogueUI.cs`: REPLACED with new versions (TextMeshPro, New Input System, items have a description). They now work with `Interactable2D`, not the old 3D `Interactable`.
- `CameraFollow2D.cs`: camera follow (was already here).
- `Assets/Rain/Editor/BlockoutSceneBuilder.cs`: menu Tools > Point and Click > build a blockout test scene, or add it to the open scene. Floor and end walls are solid; clickable objects are triggers so the player walks past them.
- Old `Interactable.cs` and `PointAndClickPlayer.cs` (3D / NavMesh / old Input) are still in the folder and unused by the new scripts. Delete them if nothing uses them.

**Scene and settings**
- URP 2D renderer (`Assets/Settings`), Input System package, Aseprite and PSD importers, 2D animation, tilemap packages.

## Broken / needs checking
1. **Plugin files showing in Git:** Unity generates `Assets/Plugins/NuGet/*` and `.claude/skills/*` after the plugin is set up. They show as hundreds of changed files. They are NOT meant to be committed unless the team agrees (see "Git rules").
2. **Unity's own Assistant window** asks for a cloud project and an `ANTHROPIC_API_KEY` (Preferences > AI > Gateway). It is a different thing from the Claude Code link. Leave it empty.
3. There are two movement scripts (`PlayerMovement2D` and `PointAndClickPlayer`). Check which one the Player object really uses before adding more movement code.

4. **New point-and-click scripts are untested.** Import TMP Essentials (Window > TextMeshPro), run the BlockoutSceneBuilder menu, press Play, check the Console. Thai text needs a Thai TMP font asset.
5. (2026-10-24) BlockoutSceneBuilder stopped after creating `Table` in the Gameplay scene (no Picture/Key/Photo/Door/GameSystems/Canvas). Likely cause: `Interactable2D.onInteract` was not initialised; fixed in code, built, untested. The builder now logs a red error and shows a dialog if it fails part-way. Delete the `BlockoutTest` object and run it again.

## Left to do / ideas
**Next, in order**
1. Click Configure for MCP in the AI Game Developer window, start `claude` in this folder, run `/mcp`, confirm the Unity server is connected.
2. Test the merged scenes from `SETTING-Scene-Gim` in play mode.
3. Push `Coding-Rain`, then open a pull request into `main` when the build works.
4. Ask the team what the jam theme / core mechanic is and write it here.

## Git rules (shared project, two people)
- Work only on `Coding-Rain`. Check the branch name before every commit.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.vs/`.
- Do not commit the generated plugin files (`Assets/Plugins/NuGet`, `.claude/skills`, `.mcp.json`) unless the team agrees.
- Two people editing the same scene or prefab causes conflicts. Say in the chat who is editing which scene.
- Merge into `main` through a pull request, not by committing to `main`.
- Stash (Branch > Stash all changes) instead of discarding when unsure.

## Build and test
- Open the project with Unity Hub, editor version **6000.5.4f1** (6000.5.9f1 is also on this PC; use the version in `ProjectSettings/ProjectVersion.txt`).
- Play: open `Assets/Scenes/...` and press Play. Watch the Console for red errors.
- Claude Code link: Window > AI Game Developer. Start with `cd "C:\Users\tsari\OneDrive\Documents\Jam-Build"` then `claude`.
- Package source: OpenUPM scoped registry `package.openupm.com` (already in `Packages/manifest.json`).
