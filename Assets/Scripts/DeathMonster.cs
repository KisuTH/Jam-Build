using System.Collections;
using UnityEngine;

/// <summary>
/// The black monster that runs at the player when the player dies.
/// Built from simple black blocks at run time (no art needed): swap this for a real sprite later.
/// Use:  yield return DeathMonster.Attack(player.transform);   (inside a coroutine)
/// </summary>
public static class DeathMonster
{
    private static Sprite square;

    private static Sprite Square()
    {
        if (square != null) return square;
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);   // 1 pixel = 1 world unit
        return square;
    }

    private static void Part(Transform parent, string name, Vector2 pos, Vector2 size, Color color, float angle = 0f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square();
        sr.color = color;
        sr.sortingOrder = 100;   // in front of everything in the world
    }

    /// <summary>
    /// The monster appears at the edge of the screen and runs at the player, faster and faster.
    /// fromSide: -1 = from the left, 1 = from the right, 0 = from the side that is farther from the player.
    /// </summary>
    public static IEnumerator Attack(Transform player, int fromSide = 0, float runTime = 0.9f)
    {
        Camera cam = Camera.main;
        if (player == null || cam == null) yield break;

        // Build the monster
        Color black = new Color(0.01f, 0.01f, 0.02f, 1f);
        GameObject root = new GameObject("Death Monster");
        Part(root.transform, "Body", new Vector2(0f, 0f), new Vector2(1.5f, 2.6f), black);
        Part(root.transform, "Head", new Vector2(0f, 1.7f), new Vector2(1.2f, 1.1f), black);
        Part(root.transform, "Arm L", new Vector2(-1.15f, 0.3f), new Vector2(0.35f, 2.6f), black, 22f);
        Part(root.transform, "Arm R", new Vector2(1.15f, 0.3f), new Vector2(0.35f, 2.6f), black, -22f);
        Part(root.transform, "Eye L", new Vector2(-0.28f, 1.8f), new Vector2(0.2f, 0.13f), new Color(1f, 0.1f, 0.1f), 12f);
        Part(root.transform, "Eye R", new Vector2(0.28f, 1.8f), new Vector2(0.2f, 0.13f), new Color(1f, 0.1f, 0.1f), -12f);
        for (int i = 0; i < 4; i++)   // jagged teeth
            Part(root.transform, "Tooth " + i, new Vector2(-0.33f + i * 0.22f, 1.45f), new Vector2(0.09f, 0.2f), new Color(0.9f, 0.9f, 0.85f));

        // Where it starts and where it runs to
        float halfWidth = cam.orthographicSize * cam.aspect;
        int side = fromSide != 0 ? (int)Mathf.Sign(fromSide) : (player.position.x >= cam.transform.position.x ? -1 : 1);
        float startX = cam.transform.position.x + side * (halfWidth + 2.5f);
        float targetX = player.position.x;

        float feetY = player.position.y;
        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null) feetY = col.bounds.min.y;
        float baseY = feetY + 1.3f;   // the monster's body is 2.6 high: its feet touch the floor

        CameraShake2D.Shake(runTime + 0.2f, 0.07f);

        // Run: slow start, very fast end
        float t = 0f;
        while (t < runTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / runTime);
            float x = Mathf.Lerp(startX, targetX, k * k);
            float bob = Mathf.Sin(Time.time * 45f) * 0.1f;
            root.transform.position = new Vector3(x, baseY + bob, 0f);
            root.transform.localScale = new Vector3(1f + Mathf.Sin(Time.time * 30f) * 0.05f, 1f, 1f);
            yield return null;
        }
        root.transform.position = new Vector3(targetX, baseY, 0f);

        // The hit: the player disappears, the monster swells for a moment
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>()) sr.enabled = false;
        CameraShake2D.Shake(0.5f, 0.35f);
        if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash(new Color(1f, 0f, 0f, 0.9f), 0.4f);
        root.transform.localScale = new Vector3(1.4f, 1.3f, 1f);
        yield return new WaitForSeconds(0.45f);

        Object.Destroy(root, 2f);
    }
}
