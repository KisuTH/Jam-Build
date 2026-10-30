using UnityEngine;

/// <summary>Walking audio follows actual grounded movement and the MAIN listener volume.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement2D))]
public sealed class PlayerFootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioClip walkingSound;
    private PlayerMovement2D movement;
    private Rigidbody2D body;
    private AudioSource source;
    private bool wasWalking;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement2D>();
        body = GetComponent<Rigidbody2D>();
        if (walkingSound == null) walkingSound = Resources.Load<AudioClip>("Audio/soumages-walking-on-wood-363349");
        if (walkingSound == null) return;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = walkingSound;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.ignoreListenerVolume = false;
        source.ignoreListenerPause = false;
    }

    private void LateUpdate()
    {
        if (source == null) return;
        bool walking = Time.timeScale > 0f && !AudioListener.pause &&
            movement.isActiveAndEnabled && movement.IsWalkingOnGround && body.simulated &&
            Mathf.Abs(body.linearVelocity.x) > .05f && Mathf.Abs(body.linearVelocity.y) < .2f;
        if (!walking)
        {
            if (wasWalking) source.Stop();
            wasWalking = false;
            return;
        }

        source.pitch = movement.IsRunning ? 1.35f : movement.IsCrouching ? .75f : 1f;
        wasWalking = true;
        if (!source.isPlaying) source.Play();
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
        wasWalking = false;
    }
}
