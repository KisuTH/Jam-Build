// Menu: Tools > Rain > Smart-Merge Gameplay Scene (Gim + Rain)
// Repairs Assets/Scenes/Gameplay.unity after the Gim merge by redoing the merge with Unity's own
// UnityYAMLMerge tool: base = common ancestor, ours = Coding-Rain (merge commit's 1st parent),
// theirs = SETTING-Scene-Gim (2nd parent). Needs the last commit to be that merge commit.
// A backup of the current scene is saved in <project>/Temp/Gameplay_before_smartmerge.unity.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SceneSmartMerge
{
    private const string ScenePath = "Assets/Scenes/Gameplay.unity";

    [MenuItem("Tools/Rain/Smart-Merge Gameplay Scene (Gim + Rain)")]
    public static void Run()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string git = FindGit();
        if (git == null) { Fail("Cannot find git.exe"); return; }

        string parents = Git(git, root, "rev-list --parents -n 1 HEAD").Trim();
        string[] p = parents.Split(' ');
        if (p.Length != 3) { Fail("The last commit is not a merge commit (parents: " + (p.Length - 1) + "). Nothing done."); return; }
        string ours = p[1], theirs = p[2];
        string baseC = Git(git, root, "merge-base " + ours + " " + theirs).Trim();
        if (baseC.Length < 7) { Fail("No merge-base found."); return; }

        string tmp = Path.Combine(root, "Temp", "ymerge");
        Directory.CreateDirectory(tmp);
        string fBase = Path.Combine(tmp, "base.unity");
        string fOurs = Path.Combine(tmp, "ours.unity");
        string fTheirs = Path.Combine(tmp, "theirs.unity");
        string fOut = Path.Combine(tmp, "result.unity");
        GitToFile(git, root, baseC + ":" + ScenePath, fBase);
        GitToFile(git, root, ours + ":" + ScenePath, fOurs);
        GitToFile(git, root, theirs + ":" + ScenePath, fTheirs);
        if (File.Exists(fOut)) File.Delete(fOut);

        string tool = Path.Combine(EditorApplication.applicationContentsPath, "Tools", "UnityYAMLMerge.exe");
        if (!File.Exists(tool)) { Fail("Cannot find " + tool); return; }

        string args = "merge -p \"" + fBase + "\" \"" + fOurs + "\" \"" + fTheirs + "\" \"" + fOut + "\"";
        int code; string o = Exec(tool, root, args, out code);
        UnityEngine.Debug.Log("UnityYAMLMerge exit code " + code + "\n" + o);

        if (!File.Exists(fOut)) { Fail("UnityYAMLMerge produced no result file (exit " + code + ")."); return; }
        string text = File.ReadAllText(fOut);
        if (Regex.IsMatch(text, @"^(<<<<<<<|>>>>>>>)", RegexOptions.Multiline))
        { Fail("The tool left real conflicts in the scene. Nothing was overwritten. Result is in " + fOut); return; }

        // duplicate-ID check
        var seen = new System.Collections.Generic.HashSet<string>();
        int dups = 0;
        foreach (Match m in Regex.Matches(text, @"^--- !u!\d+ &(\d+)", RegexOptions.Multiline))
            if (!seen.Add(m.Groups[1].Value)) dups++;
        if (dups > 0)
        { UnityEngine.Debug.LogWarning("Merged scene still has " + dups + " duplicate fileIDs (stripped prefab pairs are normal only if the count is 0)."); }

        string scene = Path.Combine(root, ScenePath);
        File.Copy(scene, Path.Combine(root, "Temp", "Gameplay_before_smartmerge.unity"), true);
        File.WriteAllText(scene, text, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        UnityEngine.Debug.Log("Gameplay.unity smart-merged OK (exit " + code + ", duplicate ids: " + dups + "). Now open the scene and press Ctrl+S.");
        EditorUtility.DisplayDialog("Smart merge", "Done (duplicate ids: " + dups + ").\nOpen Assets/Scenes/Gameplay.unity and check the Console.", "OK");
    }

    private static void Fail(string msg)
    {
        UnityEngine.Debug.LogError("Smart merge: " + msg);
        EditorUtility.DisplayDialog("Smart merge", msg, "OK");
    }

    private static string FindGit()
    {
        string[] cands =
        {
            "git",
            @"C:\Program Files\Git\cmd\git.exe",
        };
        foreach (string c in cands) { try { int code; Exec(c, null, "--version", out code); if (code == 0) return c; } catch { } }
        string gd = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "GitHubDesktop");
        if (Directory.Exists(gd))
            foreach (string dir in Directory.GetDirectories(gd, "app-*"))
            {
                string g = Path.Combine(dir, "resources", "app", "git", "cmd", "git.exe");
                if (File.Exists(g)) return g;
            }
        return null;
    }

    private static string Git(string git, string wd, string args)
    {
        int code; string o = Exec(git, wd, args, out code);
        if (code != 0) throw new Exception("git " + args + " failed: " + o);
        return o;
    }

    private static void GitToFile(string git, string wd, string spec, string file)
    {
        ProcessStartInfo psi = new ProcessStartInfo(git, "show " + spec)
        { WorkingDirectory = wd, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        using (Process pr = Process.Start(psi))
        using (FileStream fs = File.Create(file))
        {
            pr.StandardOutput.BaseStream.CopyTo(fs);
            pr.WaitForExit();
            if (pr.ExitCode != 0) throw new Exception("git show " + spec + " failed: " + pr.StandardError.ReadToEnd());
        }
    }

    private static string Exec(string exe, string wd, string args, out int code)
    {
        ProcessStartInfo psi = new ProcessStartInfo(exe, args)
        { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        if (wd != null) psi.WorkingDirectory = wd;
        using (Process pr = Process.Start(psi))
        {
            string o = pr.StandardOutput.ReadToEnd() + pr.StandardError.ReadToEnd();
            pr.WaitForExit();
            code = pr.ExitCode;
            return o;
        }
    }
}
