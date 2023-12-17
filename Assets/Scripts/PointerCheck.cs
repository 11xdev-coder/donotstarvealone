using UnityEngine;
using UnityEngine.EventSystems;

public class PointerCheck : MonoBehaviour
{
    public bool IsMouseOver()
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 localMousePosition = rt.InverseTransformPoint(Input.mousePosition);
        return rt.rect.Contains(localMousePosition);
    }
}