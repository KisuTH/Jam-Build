using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Makes scene labels that were typed in Thai only (pause menu, settings...) follow the language button.
/// Starts by itself - nothing to add to the scene. To translate another label, add a line to En below.
/// </summary>
public class UiAutoLoc : MonoBehaviour
{
    private static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        { "หยุดพักเกม", "GAME PAUSED" },
        { "ESC / เล่นต่อ", "ESC / Resume" },
        { "ความละเอียด", "Resolution" },
        { "คุณภาพกราฟิก", "Graphics Quality" },
        { "โหมดหน้าจอ", "Window Mode" },
        { "เต็มหน้าจอ", "Fullscreen" },
        { "ไร้ขอบ", "Borderless" },
        { "หน้าต่าง", "Windowed" },
        { "เล็ก", "Small" },
        { "กลาง", "Medium" },
        { "ใหญ่", "Large" },
        { "สูง", "High" },
        { "ต่ำ", "Low" },
        { "คลิกปุ่มเพื่อเปลี่ยน • Esc ยกเลิก", "Click a button to change it • Esc to cancel" },
    };

    private class Entry { public string original; public string english; }
    private readonly Dictionary<TMP_Text, Entry> cache = new Dictionary<TMP_Text, Entry>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<UiAutoLoc>() != null) return;
        GameObject go = new GameObject("UiAutoLoc");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<UiAutoLoc>();
    }

    private void OnEnable() { Loc.Changed += Apply; StartCoroutine(Loop()); }
    private void OnDisable() { Loc.Changed -= Apply; }

    private IEnumerator Loop()
    {
        var wait = new WaitForSecondsRealtime(0.3f);
        while (true) { Apply(); yield return wait; }
    }

    private static bool HasThai(string s)
    {
        foreach (char c in s) if (c >= '฀' && c <= '๿') return true;
        return false;
    }

    private static string Clean(string s)
    {
        string k = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "").Trim();
        while (k.Contains("  ")) k = k.Replace("  ", " ");
        return k;
    }

    private static string Translate(string original)
    {
        string k = Clean(original);
        if (En.TryGetValue(k, out string en)) return en;
        // "ไทย / ENGLISH" style: keep the part that has no Thai
        string[] parts = k.Split('/');
        if (parts.Length == 2)
        {
            string a = parts[0].Trim(), b = parts[1].Trim();
            if (HasThai(a) && !HasThai(b)) return b;
            if (HasThai(b) && !HasThai(a)) return a;
        }
        return null;
    }

    private void Apply()
    {
        TMP_Text[] all = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text t in all)
        {
            if (t == null || t.GetComponent<LocLabel>() != null) continue;

            if (cache.TryGetValue(t, out Entry e))
            {
                if (t.text != e.original && t.text != e.english) { cache.Remove(t); continue; }   // another script took over
            }
            else
            {
                string s = t.text;
                if (string.IsNullOrEmpty(s) || !HasThai(s)) continue;
                string en = Translate(s);
                if (en == null) continue;
                e = new Entry { original = s, english = en };
                cache[t] = e;
            }

            string want = Loc.Thai ? e.original : e.english;
            if (t.text != want) t.text = want;
        }
    }
}
