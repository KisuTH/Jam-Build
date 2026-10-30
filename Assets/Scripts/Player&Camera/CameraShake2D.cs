using UnityEngine;

/// <summary>
/// Shakes the camera. Call from anywhere:  CameraShake2D.Shake(0.5f, 0.2f);
/// (duration in seconds, strength in world units). It adds itself to the Main Camera if needed.
///
/// It works on top of any camera follow script: the shake offset is removed at the start of
/// each frame (Update) and added again at the very end (LateUpdate), so followers never see it.
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraShake2D : MonoBehaviour
{
    private Vector3 lastOffset;
    private float timeLeft;
    private float duration = 0.01f;
    private float strength;

    public static void Shake(float duration, float strength)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        CameraShake2D shake = cam.GetComponent<CameraShake2D>();
        if (shake == null) shake = cam.gameObject.AddComponent<CameraShake2D>();
        shake.StartShake(duration, strength);
    }

    private void StartShake(float newDuration, float newStrength)
    {
        duration = Mathf.Max(0.01f, newDuration);
        timeLeft = duration;
        strength = newStrength;
    }

    private void Update()
    {
        // Undo last frame's shake so the follow script works with the real position
        transform.position -= lastOffset;
        lastOffset = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f) return;

        timeLeft -= Time.deltaTime;
        float fade = Mathf.Clamp01(timeLeft / duration);   // shake gets weaker toward the end
        Vector2 random = Random.insideUnitCircle * strength * fade;

        lastOffset = new Vector3(random.x, random.y, 0f);
        transform.position += lastOffset;
    }
}
