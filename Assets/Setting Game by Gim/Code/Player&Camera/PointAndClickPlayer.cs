using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

/// <summary>
/// ตัวละครผู้เล่น: คลิกพื้นเพื่อเดิน / คลิกวัตถุเพื่อเดินไปแล้วโต้ตอบ
/// ต้องมี NavMeshAgent ติดอยู่กับ GameObject นี้
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PointAndClickPlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float interactDistance = 1.5f;

    [Header("Click Marker (optional)")]
    [SerializeField] private GameObject clickMarkerPrefab;

    private NavMeshAgent agent;
    private Interactable targetInteractable;
    private GameObject currentMarker;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleClick();
        CheckArrivedAtInteractable();
    }

    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // ไม่ทำงานถ้าคลิกโดน UI (เช่น ปุ่ม Inventory)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 1) ลองคลิกวัตถุที่โต้ตอบได้ก่อน
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                targetInteractable = interactable;
                MoveTo(interactable.GetInteractPosition());
                return;
            }
        }

        // 2) ไม่ใช่วัตถุ -> เดินไปที่พื้น
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            targetInteractable = null;
            MoveTo(hit.point);
            ShowMarker(hit.point);
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    private void CheckArrivedAtInteractable()
    {
        if (targetInteractable == null) return;
        if (agent.pathPending) return;

        float dist = Vector3.Distance(transform.position, targetInteractable.transform.position);
        if (agent.remainingDistance <= interactDistance || dist <= interactDistance)
        {
            agent.ResetPath();
            Interactable t = targetInteractable;
            targetInteractable = null;
            t.Interact(this);
        }
    }

    private void ShowMarker(Vector3 pos)
    {
        if (clickMarkerPrefab == null) return;
        if (currentMarker != null) Destroy(currentMarker);
        currentMarker = Instantiate(clickMarkerPrefab, pos + Vector3.up * 0.05f, Quaternion.identity);
        Destroy(currentMarker, 1f);
    }
}
