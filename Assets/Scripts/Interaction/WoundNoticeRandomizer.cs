using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The three "Wound Care Instructions" papers: every time the scene loads (every loop) ONE of them, picked at random,
/// carries the real order of the dressing puzzle. The other two are only torn / unreadable notices.
/// No setup: it creates itself when the game starts and works on the papers already in the scene.
/// </summary>
public static class WoundNoticeRandomizer
{
    private const string PaperName = "Wound Care Instructions";

    private const string RealEn =
        "An old notice: WOUND CARE ORDER. 1. Prepare the tray. 2. Place the bandage. 3. Prepare the wound tools (scissors / forceps). 4. Prepare the medicine / disinfectant. - Public Health Officer";
    private const string RealTh =
        "เอกสารเก่า: คำสั่งการทำแผล 1. เตรียมถาด 2. ใส่ผ้าพันแผล 3. เตรียมอุปกรณ์ทำแผล (กรรไกร / คีม) 4. เตรียมยา / น้ำยาฆ่าเชื้อ - เจ้าหน้าที่สาธารณสุข";

    private static readonly string[][] Fake =
    {
        new[] { "A torn notice. Almost all of it is gone. Only a few words are left: \"...wash your hands...\"",
                "ประกาศที่ฉีกขาด เกือบทั้งแผ่นหายไป เหลือเพียงไม่กี่คำ: \"...ล้างมือให้สะอาด...\"" },
        new[] { "A faded notice. The ink has run and the water stains cover every line. Nothing can be read.",
                "ประกาศที่ซีดจาง หมึกเลอะและมีคราบน้ำทับทุกบรรทัด อ่านอะไรไม่ออกเลย" },
        new[] { "An old notice about visiting hours. Nothing about treating wounds.",
                "ประกาศเก่าเรื่องเวลาเยี่ยมไข้ ไม่มีอะไรเกี่ยวกับการทำแผลเลย" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // also run again every time the scene reloads (every loop), so a new paper is picked each time
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Apply();
    }

    private static void Apply()
    {
        var papers = new List<Interactable2D>();
        foreach (Interactable2D it in Object.FindObjectsByType<Interactable2D>(FindObjectsInactive.Include))
            if (it != null && it.displayName == PaperName) papers.Add(it);
        if (papers.Count == 0) return;
        papers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        int real = Random.Range(0, papers.Count);
        // the other papers get different fake texts
        var pool = new List<int>();
        for (int i = 0; i < Fake.Length; i++) pool.Add(i);

        for (int i = 0; i < papers.Count; i++)
        {
            Interactable2D p = papers[i];
            if (i == real)
            {
                p.description = RealEn;
                p.descriptionThai = Loc.Shape(RealTh);
                p.clueId = "wound_order";
                p.clueTitle = "Wound care order";
                p.clueText = "Wound care order: 1 tray, 2 bandage, 3 tools (scissors), 4 medicine / disinfectant.";
            }
            else
            {
                int k = Random.Range(0, pool.Count);
                string[] f = Fake[pool[k]];
                pool.RemoveAt(k);
                p.description = f[0];
                p.descriptionThai = Loc.Shape(f[1]);
                p.clueId = "";          // only the real paper adds a page to the Case Book
                p.clueTitle = "";
                p.clueText = "";
            }
        }
    }
}
