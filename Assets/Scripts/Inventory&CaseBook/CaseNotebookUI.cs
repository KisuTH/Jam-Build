using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The case book, shown like a real book: ONE clue per page, with Prev / Next buttons.
///   Tab (or the Case Book button) = open / close
///   Left / Right arrow keys, or the Prev / Next buttons = turn the page
/// (A and D are NOT used here because they walk the player.)
///
/// Put this on an object that stays ACTIVE (NOT on the panel itself, because the
/// panel is hidden when closed). Assign:
///   panel        = the Panel that holds the pages (starts hidden)
///   titleText    = (optional) TMP text for the clue title. Empty = the title goes inside entriesText
///   entriesText  = TMP text for the clue text
///   pageText     = (optional) TMP text that shows "2 / 5"
///   prevButton / nextButton = (optional) page turn buttons
///   openButton   = (optional) button that opens / closes the book
///   buttonLabel  = (optional) text on that button, shows how many clues you have
/// </summary>
public class CaseNotebookUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text entriesText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button openButton;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Key toggleKey = Key.Tab;

    private int currentPage;      // 0 = first clue
    private int lastSeenCount;    // how many clues existed the last time the book was opened

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (openButton != null) openButton.onClick.AddListener(Toggle);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);

        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.OnChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (CaseNotebook.Instance != null)
            CaseNotebook.Instance.OnChanged -= Refresh;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[toggleKey].wasPressedThisFrame) Toggle();

        // Page turning with the arrow keys, only while the book is open
        if (panel != null && panel.activeSelf)
        {
            if (kb.leftArrowKey.wasPressedThisFrame) PreviousPage();
            if (kb.rightArrowKey.wasPressedThisFrame) NextPage();
        }
    }

    public void Toggle()
    {
        if (panel == null) return;

        bool opening = !panel.activeSelf;
        panel.SetActive(opening);

        if (opening)
        {
            // If there are new clues since last time, open on the newest one
            int count = Count();
            if (count > lastSeenCount) currentPage = count - 1;
            lastSeenCount = count;
            Refresh();
        }
    }

    public void NextPage()
    {
        currentPage++;
        Refresh();
    }

    public void PreviousPage()
    {
        currentPage--;
        Refresh();
    }

    private int Count()
    {
        CaseNotebook book = CaseNotebook.Instance;
        return book != null ? book.Entries.Count : 0;
    }

    // ===== "View document" button: only on pages whose clue has a document picture =====
    private GameObject docButton;
    private Sprite docSprite;

    private void ShowDocumentButton(Sprite image)
    {
        docSprite = image;
        if (image == null)
        {
            if (docButton != null) docButton.SetActive(false);
            return;
        }
        if (panel == null) return;
        if (docButton == null)
        {
            docButton = new GameObject("ViewDocumentButton", typeof(RectTransform), typeof(Image), typeof(Button));
            docButton.transform.SetParent(panel.transform, false);
            RectTransform rt = docButton.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(340f, 64f);
            rt.anchoredPosition = new Vector2(0f, 36f);
            docButton.GetComponent<Image>().color = new Color(0.36f, 0.12f, 0.1f, 0.95f);
            docButton.GetComponent<Button>().onClick.AddListener(() => BoardClue.ViewDocument(docSprite));

            GameObject lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(docButton.transform, false);
            RectTransform lr = lbl.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
            TextMeshProUGUI t = lbl.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = 30;
            t.color = Color.white;
            t.raycastTarget = false;
            t.text = Loc.T("View document");
        }
        else docButton.GetComponentInChildren<TMP_Text>().text = Loc.T("View document");
        docButton.SetActive(true);
        docButton.transform.SetAsLastSibling();
    }

    private void Refresh()
    {
        int count = Count();
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, count - 1));

        if (buttonLabel != null)
            buttonLabel.text = "Case Book [Tab] (" + count + ")";

        // Page number and arrows
        if (pageText != null)
            pageText.text = count == 0 ? "0 / 0" : (currentPage + 1) + " / " + count;
        if (prevButton != null) prevButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < count - 1;

        // The clue itself
        if (count == 0)
        {
            ShowDocumentButton(null);
            if (titleText != null) titleText.text = "";
            if (entriesText != null)
                entriesText.text = "(No clues yet. Look around and click things.)";
            return;
        }

        ClueEntry e = CaseNotebook.Instance.Entries[currentPage];
        ShowDocumentButton(e.image);

        if (titleText != null)
        {
            titleText.text = e.title;
            if (entriesText != null) entriesText.text = e.text;
        }
        else if (entriesText != null)
        {
            // No separate title text: show the title in bold above the clue
            entriesText.text = "<b>" + e.title + "</b>\n\n" + e.text;
        }
    }
}
