using System.Collections;
using UnityEngine;

/// <summary>The authored shadow character runs at the player when the player dies.</summary>
public static class DeathMonster
{
    /// <summary>
    /// The monster appears at the edge of the screen and runs at the player, faster and faster.
    /// fromSide: -1 = from the left, 1 = from the right, 0 = from the side that is farther from the player.
    /// </summary>
    public static IEnumerator Attack(Transform player, int fromSide = 0, float runTime = 0.9f)
    {
        Camera cam = Camera.main;
        if (player == null || cam == null) yield break;

        GameObject root = ShadowEnemyVisual.CreateWorld();
        if (root == null) yield break;
        root.name = "Death Monster";
        SpriteRenderer visual = root.GetComponent<SpriteRenderer>();
        Vector3 baseScale = root.transform.localScale;

        float halfWidth = cam.orthographicSize * cam.aspect;
        int side = fromSide != 0 ? (int)Mathf.Sign(fromSide) : (player.position.x >= cam.transform.position.x ? -1 : 1);
        float startX = cam.transform.position.x + side * (halfWidth + visual.bounds.extents.x);
        float targetX = player.position.x;
        visual.flipX = side < 0;

        float feetY = player.position.y;
        Collider2D col = player.GetComponent<Collider2D>();
        if (col != null) feetY = col.bounds.min.y;

        CameraShake2D.Shake(runTime + 0.2f, 0.07f);

        // Run: slow start, very fast end. Keep the authored size and align its feet to the floor.
        float t = 0f;
        while (t < runTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / runTime);
            float x = Mathf.Lerp(startX, targetX, k * k);
            float bob = Mathf.Sin(Time.time * 45f) * 0.1f;
            root.transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + Mathf.Sin(Time.time * 30f) * 0.05f, 1f, 1f));
            PlaceFeet(visual, x, feetY + bob, player.position.z);
            yield return null;
        }
        PlaceFeet(visual, targetX, feetY, player.position.z);

        // The hit: the player disappears, the monster swells for a moment.
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>()) sr.enabled = false;
        CameraShake2D.Shake(0.5f, 0.35f);
        if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash(new Color(1f, 0f, 0f, 0.9f), 0.4f);
        root.transform.localScale = Vector3.Scale(baseScale, new Vector3(1.4f, 1.3f, 1f));
        PlaceFeet(visual, targetX, feetY, player.position.z);
        yield return new WaitForSeconds(0.45f);

        Object.Destroy(root, 2f);
    }

    private static void PlaceFeet(SpriteRenderer visual, float x, float feetY, float z)
    {
        visual.transform.position = new Vector3(x, feetY, z);
        visual.transform.position += Vector3.up * (feetY - visual.bounds.min.y);
    }
}
