using System.Collections;
using UnityEngine;

/// <summary>
/// A short jump scare in the dressing room: the player takes the red file and reads the paper with the code
/// (story flag "got_cursed_red_file"). When the paper is closed a ghost pops up near the player, a sound plays,
/// the screen shakes, then the ghost fades away. It happens once per life.
/// Tools > Rain > Setup Code Paper Scare builds the object.
/// </summary>
public class CodeScareEvent : MonoBehaviour
{
    [Header("When")]
    [Tooltip("The story flag that starts the scare (taking the red file from the drawer).")]
    public string triggerFlag = "got_cursed_red_file";
    [Tooltip("After the code paper is closed, wait this long before the ghost appears.")]
    public float delayAfterPaper = 0.4f;

    [Header("Ghost")]
    public GameObject ghost;
    [Tooltip("Where the ghost appears (only its X is used). Empty = near the player.")]
    public Transform spawnPoint;
    [Tooltip("How far from the player the ghost appears (towards the middle of the room).")]
    public float appearDistance = 3.5f;
    public float fadeInTime = 0.12f;
    [Tooltip("How long the ghost stays.")]
    public float stayTime = 1.2f;
    public float fadeOutTime = 0.6f;
    public float feetOffset = 0f;
    public bool spriteFacesLeft = true;

    [Header("Effects")]
    public AudioClip scareSound;
    [Range(0f, 1f)] public float soundVolume = 1f;
    public float shakeTime = 0.5f;
    public float shakeStrength = 0.2f;

    private bool done;
    private SpriteRenderer[] renderers;
    private float alpha;

    private void Start()
    {
        if (ghost != null) { renderers = ghost.GetComponentsInChildren<SpriteRenderer>(true); ghost.SetActive(false); }
        if (StoryFlags.Instance != null) StoryFlags.Instance.OnFlagSet += OnFlag;
    }

    private void OnDestroy()
    {
        if (StoryFlags.Instance != null) StoryFlags.Instance.OnFlagSet -= OnFlag;
    }

    private void OnFlag(string flag)
    {
        if (done || flag != triggerFlag || ghost == null) return;
        done = true;
        StartCoroutine(Scare());
    }

    private void SetAlpha(float a)
    {
        alpha = Mathf.Clamp01(a);
        Apply();
    }

    private void Apply()
    {
        if (renderers == null) return;
        foreach (SpriteRenderer r in renderers)
        {
            if (r == null) continue;
            Color c = r.color; c.a = alpha; r.color = c;
        }
    }

    private void LateUpdate()
    {
        if (ghost != null && ghost.activeSelf) Apply();
    }

    private IEnumerator Scare()
    {
        // wait until the code paper has been read and closed
        yield return null;
        while (GameObject.Find("BoardZoom") != null || GameObject.Find("BoardDocument") != null) yield return new WaitForSeconds(0.1f);
        yield return new WaitForSeconds(delayAfterPaper);

        GameObject pg = GameObject.FindWithTag("Player");
        if (pg == null) yield break;
        Transform player = pg.transform;
        Vector2 p = player.position;

        // appear towards the middle of the room, inside the room
        float dir = 1f, minX = p.x - appearDistance - 1f, maxX = p.x + appearDistance + 1f;
        Room room = RoomAt(p);
        if (room != null)
        {
            dir = p.x < (room.boundsMin.x + room.boundsMax.x) * 0.5f ? 1f : -1f;
            minX = room.boundsMin.x + 1f; maxX = room.boundsMax.x - 1f;
        }
        float x = spawnPoint != null ? spawnPoint.position.x : Mathf.Clamp(p.x + dir * appearDistance, minX, maxX);

        Collider2D col = pg.GetComponent<Collider2D>();
        float groundY = (col != null ? col.bounds.min.y : p.y) + feetOffset;

        SetAlpha(0f);
        ghost.transform.position = new Vector3(x, p.y, 0f);
        ghost.SetActive(true);
        SpriteRenderer sr = ghost.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            sr.flipX = ((p.x - x) > 0f) != !spriteFacesLeft;
            Vector3 pos = ghost.transform.position;
            pos.y += groundY - sr.bounds.min.y;
            ghost.transform.position = pos;
        }

        GameSfx.Play(scareSound, soundVolume);
        CameraShake2D.Shake(shakeTime, shakeStrength);

        for (float t = 0f; t < fadeInTime; t += Time.deltaTime) { SetAlpha(t / Mathf.Max(0.01f, fadeInTime)); yield return null; }
        SetAlpha(1f);
        yield return new WaitForSeconds(stayTime);
        for (float t = 0f; t < fadeOutTime; t += Time.deltaTime) { SetAlpha(1f - t / Mathf.Max(0.01f, fadeOutTime)); yield return null; }
        SetAlpha(0f);
        ghost.SetActive(false);
    }

    private static Room RoomAt(Vector2 pos)
    {
        Room best = null;
        float bestArea = float.MaxValue;
        foreach (Room r in Room.All)
        {
            if (pos.x < r.boundsMin.x || pos.x > r.boundsMax.x) continue;
            if (pos.y < Mathf.Min(r.boundsMin.y, r.boundsMax.y) || pos.y > Mathf.Max(r.boundsMin.y, r.boundsMax.y)) continue;
            float a = Mathf.Abs((r.boundsMax.x - r.boundsMin.x) * (r.boundsMax.y - r.boundsMin.y));
            if (a < bestArea) { bestArea = a; best = r; }
        }
        return best;
    }
}
