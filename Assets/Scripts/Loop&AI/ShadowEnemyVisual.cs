using UnityEngine;

/// <summary>Shared authored shadow character for death attacks and keypad threats.</summary>
public static class ShadowEnemyVisual
{
    private static GameObject prefab;

    private static GameObject Prefab
    {
        get
        {
            if (prefab == null) prefab = Resources.Load<GameObject>("Enemies/ShadowEnemy");
            return prefab;
        }
    }

    public static GameObject CreateWorld()
    {
        if (Prefab == null)
        {
            Debug.LogError("Missing Enemies/ShadowEnemy visual prefab.");
            return null;
        }
        return Object.Instantiate(Prefab);
    }

    // The source artwork faces left; a threat approaching from the left faces right.
    public static void ApplyToUI(GameObject target, int fromSide)
    {
        if (target == null) return;
        UnityEngine.UI.Image image = target.GetComponent<UnityEngine.UI.Image>();
        if (image == null) return;
        SpriteRenderer source = Prefab != null ? Prefab.GetComponent<SpriteRenderer>() : null;
        image.sprite = source != null ? source.sprite : null;
        image.enabled = image.sprite != null;
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.color = Color.white;
        image.material = null;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Vector3 scale = target.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (fromSide < 0 ? -1f : 1f);
        target.transform.localScale = scale;

        // The older scene UI has separate red-eye squares belonging to its placeholder.
        foreach (Transform child in target.transform)
            if (child.name == "Eyes" || child.name.StartsWith("Eye ")) child.gameObject.SetActive(false);
    }
}
