using UnityEngine;
using UnityEngine.EventSystems;

// Switches between the credits desk and the supplied full-screen member cards.
public sealed class CreditsGallery : MonoBehaviour
{
    [SerializeField] private GameObject overviewRoot;
    [SerializeField] private GameObject detailRoot;
    [SerializeField] private UnityEngine.UI.Image detailImage;
    [SerializeField] private Sprite[] memberCards = new Sprite[0];

    private void OnEnable() => BackToOverview();

    public void ShowMember(int index)
    {
        if (index < 0 || index >= memberCards.Length || memberCards[index] == null) return;
        detailImage.sprite = memberCards[index];
        overviewRoot.SetActive(false);
        detailRoot.SetActive(true);
        ClearSelection();
    }

    public void BackToOverview()
    {
        if (detailRoot != null) detailRoot.SetActive(false);
        if (detailImage != null) detailImage.sprite = null;
        if (overviewRoot != null) overviewRoot.SetActive(true);
        ClearSelection();
    }

    private static void ClearSelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}
