# Jam-Build: instructions for Claude Code

Unity 6 (6000.5.4f1) 2D game jam project, made with a friend. The user (KisuTH) is the programmer. Keep scripts simple and well commented: they are still learning C# and Unity.

## Start of every session
1. Read `PROGRESS.md` (the summary and the to-do lists).
2. Check the Git branch with `git branch --show-current`. It must be `Coding-Rain`. If it is not, stop and tell the user.

## While working
- Use the Unity MCP tools (the `unity` server) to inspect and change scenes when they are connected; otherwise edit the C# files directly.
- Put new scripts in `Assets/Scripts`. Never edit `.meta` files by hand.
- Do not touch a scene or prefab the friend is working on without asking. Do not edit `ProjectSettings` or `Packages/manifest.json` unless asked.
- Tell the user what to test in Unity after each change, because Claude cannot see play mode.

## After every change
- Update `PROGRESS.md`: "Status at a glance", the right area, and "Broken / needs checking" or "Left to do". Mark things "built, untested" until the user says they work. Add the date.
- Offer a short commit message. Only commit when the user says so. Never push or merge into `main` unless asked.

## Git rules
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.vs/`.
- Do not commit generated plugin files (`Assets/Plugins/NuGet`, `.claude/skills`, `.mcp.json`) unless the user says the team agreed.
- Never discard (`git checkout .`, `git reset --hard`) uncommitted work: stash instead, and ask first.
