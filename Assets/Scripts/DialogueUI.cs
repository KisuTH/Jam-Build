using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// กล่องข้อความด้านล่างจอ พิมพ์ทีละตัวอักษร คลิกเพื่อข้ามหรือปิด
/// ใช้ Text ธรรมดา (UnityEngine.UI.Text) ถ้าใช้ TextMeshPro ให้เปลี่ยนเป็น TMP_Text
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text dialogueText;
    [SerializeField] private float charsPerSecond = 40f;
    [SerializeField] private float autoCloseDelay = 2.5f;

    private Coroutine routine;
    private string fullText;
    private bool typing;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (typing)
            {
                // ข้ามการพิมพ์ แสดงข้อความเต็ม
                StopCoroutine(routine);
                dialogueText.text = fullText;
                typing = false;
                routine = StartCoroutine(AutoClose());
            }
        }
    }

    public void Show(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (routine != null) StopCoroutine(routine);

        fullText = text;
        panel.SetActive(true);
        routine = StartCoroutine(Typewriter());
    }

    public void Hide()
    {
        if (routine != null) StopCoroutine(routine);
        typing = false;
        panel.SetActive(false);
    }

    private IEnumerator Typewriter()
    {
        typing = true;
        dialogueText.text = "";
        float delay = 1f / charsPerSecond;

        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        typing = false;
        routine = StartCoroutine(AutoClose());
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        panel.SetActive(false);
    }
}
