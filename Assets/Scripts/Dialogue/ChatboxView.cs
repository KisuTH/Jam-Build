using TMPro;
using UnityEngine;

/// <summary>Shared artwork, speaker heading and hint colours for dialogue and phone calls.</summary>
public sealed class ChatboxView : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Material hintMaterial;
    private static readonly Color SpeakerColor = new Color(.96f, .86f, .7f);
    private static readonly Color HintSpeakerColor = new Color(1f, .55f, .48f);

    public void Present(string speaker, bool isHint)
    {
        if (background != null) background.material = isHint ? hintMaterial : null;
        if (speakerText != null)
        {
            speakerText.text = string.IsNullOrEmpty(speaker)
                ? (Loc.Thai ? Loc.Shape("บรรยาย") : "Narration") : Loc.T(speaker);
            speakerText.color = isHint ? HintSpeakerColor : SpeakerColor;
            speakerText.maxVisibleCharacters = int.MaxValue;
        }
        if (bodyText != null) bodyText.color = isHint
            ? new Color(1f, .91f, .88f) : new Color(.96f, .94f, .9f);
    }
}
