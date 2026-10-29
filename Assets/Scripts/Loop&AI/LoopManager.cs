using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The "Return by Death" loop (like Re-Zero). Put ONE in the scene (the builder adds it to GameSystems).
///
///  - Makes a random passcode (4 digits). The digits are hidden in several places (CodeClue objects).
///  - If the player fails (wrong code, wrong way), the AI phones to insult the player, then the player dies.
///  - Death = the scene reloads, the loop counter goes up and a NEW random code is made.
///
/// The loop number and the code are static, so they survive the scene reload.
/// IMPORTANT: save the scene (Ctrl+S) before pressing Play, because the loop reloads it from disk.
/// </summary>
public class LoopManager : MonoBehaviour
{
    public static LoopManager Instance { get; private set; }

    public const int CodeLength = 4;
    /// <summary>How many places in the level CAN hide a digit. Each loop only CodeLength of them really do.</summary>
    public const int SpotCount = 8;
    /// <summary>The code spot that is the dressing-room drawer (always hides one digit; the notice boards 0..6 hide the other CodeLength-1).</summary>
    public const int DrawerSlot = 7;
    public const string DrawerLocation = "the drawer in the dressing room (it opens after the wound puzzle)";

    /// <summary>Which loop we are in (1 = the first life).</summary>
    public static int LoopCount = 1;
    /// <summary>The secret code of this loop, one digit per entry.</summary>
    public static int[] Code;
    /// <summary>
    /// For each code spot (0..SpotCount-1): which digit position it shows (0 = first digit),
    /// or -1 = nothing here this loop. Different spots are chosen every loop.
    /// </summary>
    public static int[] SpotPosition;

    // Which code spots exist in the scene right now (CodeClue / BoardClue register here).
    // Digits are only given to spots that exist, so a scene with only 5 boards still works.
    private static readonly int[] registered = new int[SpotCount];
    /// <summary>Where each spot is (set by BoardClue), used by the hint system.</summary>
    public static readonly string[] SlotLocation = new string[SpotCount];

    public static void RegisterSlot(int slot, string location = null)
    {
        slot = Mathf.Clamp(slot, 0, SpotCount - 1);
        registered[slot]++;
        if (!string.IsNullOrEmpty(location)) SlotLocation[slot] = location;
    }
    public static void UnregisterSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 0, SpotCount - 1);
        if (registered[slot] > 0) registered[slot]--;
    }

    [Header("Death screen")]
    [Tooltip("Big text in the middle of the screen (TextMeshPro, can be empty at start).")]
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private Color deathFadeColor = new Color(0.25f, 0f, 0f, 1f);
    [SerializeField] private float deathScreenTime = 2.8f;

    private bool dying;
    private DeathTransitionUI activeTransition;
    private static DeathTransitionUI pendingTransition;
    private readonly bool[] foundSlots = new bool[SpotCount];

    public bool IsDying => dying;

    /// <summary>Has the player already read a clue that shows this digit position (0 = first digit)?</summary>
    public bool IsPositionFound(int position)
    {
        if (SpotPosition == null) return false;
        for (int i = 0; i < SpotCount; i++)
            if (SpotPosition[i] == position && foundSlots[i]) return true;
        return false;
    }

    /// <summary>A CodeClue was read (used by the hint system).</summary>
    public void MarkFound(int slot)
    {
        if (slot >= 0 && slot < SpotCount) foundSlots[slot] = true;
    }

    /// <summary>A random code spot that really hides a digit and has NOT been read yet, or -1 if all are found.</summary>
    public int PickUnfoundSlot()
    {
        if (SpotPosition == null) GenerateCode();
        System.Collections.Generic.List<int> open = new System.Collections.Generic.List<int>();
        for (int i = 0; i < SpotCount; i++)
            if (SpotPosition[i] >= 0 && !foundSlots[i]) open.Add(i);
        return open.Count == 0 ? -1 : open[Random.Range(0, open.Count)];
    }

    // New Play session in the editor: start again from loop 1
    public static void BeginNewGame()
    {
        ResetStatics();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        LoopCount = 1;
        pendingTransition = null;
        Code = null;
        SpotPosition = null;
        for (int i = 0; i < SpotCount; i++) { registered[i] = 0; SlotLocation[i] = null; }
        SlotLocation[DrawerSlot] = DrawerLocation;
    }

    /// <summary>
    /// The LoopManager of the scene. If the scene has none (for example the builder was not run),
    /// one is created automatically, so wrong codes / wrong ways / hints still work.
    /// </summary>
    public static LoopManager Ensure()
    {
        if (Instance != null) return Instance;

        LoopManager found = FindAnyObjectByType<LoopManager>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            return found;
        }

        Debug.Log("LoopManager: none in the scene, so one was created automatically.");
        GameObject go = new GameObject("LoopManager (auto)");
        return go.AddComponent<LoopManager>();
    }

    private void Awake()
    {
        Instance = this;
        if (pendingTransition != null)
        {
            activeTransition = pendingTransition;
            pendingTransition = null;
            dying = true;
            SetPlayerLocked(true);
        }

        // (the code is made lazily, after every clue has registered)

        // The death text is found by name if it was not assigned
        if (deathText == null)
        {
            GameObject t = GameObject.Find("DeathText");
            if (t != null) deathText = t.GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        if (Code == null) GenerateCode();
        if (deathText != null) deathText.text = "";
        if (LoopCount > 1) StartCoroutine(Intro());
    }

    // ===== The code =====

    private static void GenerateCode()
    {
        Code = new int[CodeLength];
        for (int i = 0; i < CodeLength; i++) Code[i] = Random.Range(0, 10);

        // ONE digit is always in the dressing-room drawer; the other CodeLength-1 are on different notice boards (0..DrawerSlot-1)
        int boardsNeeded = CodeLength - 1;
        System.Collections.Generic.List<int> spots = new System.Collections.Generic.List<int>();
        for (int i = 0; i < DrawerSlot; i++) if (registered[i] > 0) spots.Add(i);
        if (spots.Count < boardsNeeded)   // not enough boards in the scene: fall back to all of them
        {
            spots.Clear();
            for (int i = 0; i < DrawerSlot; i++) spots.Add(i);
        }
        Shuffle(spots);

        System.Collections.Generic.List<int> positions = new System.Collections.Generic.List<int>();
        for (int i = 0; i < CodeLength; i++) positions.Add(i);
        Shuffle(positions);

        SpotPosition = new int[SpotCount];
        for (int i = 0; i < SpotCount; i++) SpotPosition[i] = -1;       // -1 = nothing here
        SpotPosition[DrawerSlot] = positions[0];                        // the drawer's digit
        for (int i = 0; i < boardsNeeded; i++) SpotPosition[spots[i]] = positions[i + 1];
        SlotLocation[DrawerSlot] = DrawerLocation;
    }

    private static void Shuffle(System.Collections.Generic.List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    /// <summary>The digit position (0 = first) that code spot number "slot" shows, or -1 if nothing is hidden there this loop.</summary>
    public static int PositionForSlot(int slot)
    {
        if (SpotPosition == null) GenerateCode();
        return SpotPosition[Mathf.Clamp(slot, 0, SpotCount - 1)];
    }

    /// <summary>The digit that code spot number "slot" shows (-1 if none).</summary>
    public static int DigitForSlot(int slot)
    {
        int position = PositionForSlot(slot);
        return position < 0 ? -1 : Code[position];
    }

    public static bool IsCorrect(string entered)
    {
        if (Code == null || entered == null || entered.Length != CodeLength) return false;
        for (int i = 0; i < CodeLength; i++)
            if (entered[i] - '0' != Code[i]) return false;
        return true;
    }

    // ===== Failing and dying =====

    /// <summary>The AI phones and insults the player, then the player dies.</summary>
    public void FailPuzzle(CallSituation situation, int monsterFromSide = 0)
    {
        if (dying) return;
        dying = true;
        StartCoroutine(FailRoutine(situation, monsterFromSide));
    }

    /// <summary>The player dies right now (no phone call).</summary>
    public void Die(int monsterFromSide = 0)
    {
        if (dying) return;
        dying = true;
        StartCoroutine(DieRoutine(monsterFromSide));
    }

    private IEnumerator FailRoutine(CallSituation situation, int monsterFromSide)
    {
        if (AICaller.Instance != null)
            yield return AICaller.Instance.CallRoutine(situation, 2, true);
        yield return DieRoutine(monsterFromSide);
    }

    private IEnumerator DieRoutine(int monsterFromSide = 0)
    {
        SetPlayerLocked(true);
        if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();

        // The black monster runs at the player and kills them
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) yield return DeathMonster.Attack(player.transform, monsterFromSide);

        CameraShake2D.Shake(0.6f, 0.3f);
        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(new Color(0.8f, 0f, 0f, 0.9f), 0.5f);
            yield return new WaitForSeconds(0.5f);
            ScreenFlash.Instance.FadeToColor(deathFadeColor, 1f);
            yield return new WaitForSeconds(1f);
        }

        if (deathText != null) deathText.text = "";
        activeTransition = DeathTransitionUI.Create(transform, deathText, deathFadeColor);
        yield return activeTransition.Play(LoopCount, LoopCount + 1, deathScreenTime);

        // Commit the next life only after the old number has rolled out and the impact finishes.
        LoopCount++;
        GenerateCode();
        activeTransition.KeepAcrossReload();
        pendingTransition = activeTransition;
        activeTransition = null;
        ReloadScene();
    }

    private IEnumerator Intro()
    {
        SetPlayerLocked(true);
        if (activeTransition != null)
        {
            yield return activeTransition.RevealGameplay();
            Destroy(activeTransition.gameObject);
            activeTransition = null;
        }
        else
        {
            // Compatibility for a scene started directly with an existing loop count.
            if (ScreenFlash.Instance != null)
            {
                ScreenFlash.Instance.FadeToColor(Color.black, 0.01f);
                yield return null;
                yield return new WaitForSeconds(0.3f);
                ScreenFlash.Instance.FadeToClear(1.2f);
            }
            yield return new WaitForSeconds(1.2f);
        }
        dying = false;
        if (AICaller.Instance != null)
            yield return AICaller.Instance.CallRoutine(CallSituation.LoopStart, 2, false);
        else
            SetPlayerLocked(false);
    }

    private void OnDestroy()
    {
        if (activeTransition != null) Destroy(activeTransition.gameObject);
        if (Instance == this) Instance = null;
    }

    private static void ReloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        // A scene that is not in Build Settings cannot be loaded by LoadScene while testing in the editor
        if (scene.buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        if (scene.buildIndex < 0)
        {
            Debug.LogError("LoopManager: add this scene to Build Settings so it can restart.");
            return;
        }
        SceneManager.LoadScene(scene.buildIndex);
    }

    // ===== Helper: lock / unlock the player's keys =====

    public static void SetPlayerLocked(bool locked)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        PlayerMovement2D move = player.GetComponent<PlayerMovement2D>();
        if (move != null) move.SetControlLocked(locked);
    }
}
