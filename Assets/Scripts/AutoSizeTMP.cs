using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class AutoSizeTMP : MonoBehaviour
{
    private TextMeshProUGUI _tmpText;
    private RectTransform _rectTransform;

    void Awake()
    {
        _tmpText = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Adjust the size of the RectTransform to fit the text
        Vector2 newSize = new Vector2(_tmpText.preferredWidth, _tmpText.preferredHeight);
        _rectTransform.sizeDelta = newSize;
    }

    public void UpdateText(string newText)
    {
        _tmpText.text = newText;
        // Optionally, force an immediate update of the TMP layout
        _tmpText.ForceMeshUpdate();
    }
}