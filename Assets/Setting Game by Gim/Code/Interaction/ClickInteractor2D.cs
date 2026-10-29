using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Put ONE of these in the scene (e.g. on a "GameSystems" object).
/// Left-click on an object with Interactable2D to interact with it.
/// </summary>
public class ClickInteractor2D : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [Tooltip("The player. Used for the distance check.")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask interactableLayers = ~0;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // Ignore all clicks while a door transition runs (and its short cooldown)
        if (RoomTransition.Instance != null && RoomTransition.Instance.IsBusy) return;

        // Ignore clicks while a dialogue is playing (the click is used to read on)
        if (DialogueSequence.IsAnyPlaying) return;

        // Ignore clicks on UI (inventory slots, dialogue box)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Collider2D[] hits = Physics2D.OverlapPointAll(world, interactableLayers);

        foreach (Collider2D hit in hits)
        {
            Interactable2D target = hit.GetComponentInParent<Interactable2D>();
            if (target != null)
            {
                target.Interact(player);
                return;
            }
        }
    }
}
