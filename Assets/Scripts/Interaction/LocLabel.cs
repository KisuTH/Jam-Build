using TMPro;
using UnityEngine;

/// <summary>World-space text that switches between English and Thai with the language button.</summary>
[RequireComponent(typeof(TMP_Text))]
public class LocLabel : MonoBehaviour
{
    public string english = "";
    public string thai = "";

    private TMP_Text label;

    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        Loc.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() { Loc.Changed -= Refresh; }

    private void Refresh()
    {
        if (label == null) return;
        label.text = Loc.Thai && !string.IsNullOrEmpty(thai) ? Loc.Shape(thai) : english;
    }
}
