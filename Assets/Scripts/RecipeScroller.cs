using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecipeScroller : MonoBehaviour, IScrollHandler
{
    [Header("Main")]
    public RectTransform contentPanel;
    public RecipeLoader recipeLoader;
    public GameObject clickedButton;
    
    [Header("Scrolling Config")]
    public float buttonHeight = 30f;
    public float spacing = 5f;
    
    private float _scrollAmount;
    private float _minScrollPosition;
    private float _maxScrollPosition;
    private GameObject _previousButton;
    private GameObject _currentButton;
    private bool _isScrolling;

    private void Start()
    {
        // calculate scroll amount
        _scrollAmount = buttonHeight + spacing;
        // calculate scroll limits
        CalculateScrollLimits();
    }

    private void CalculateScrollLimits()
    {
        _maxScrollPosition = Mathf.Max(0, contentPanel.sizeDelta.y - buttonHeight); 
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_currentButton != null)
        {
            recipeLoader.HideMaterialsInstantly(_currentButton);
            _currentButton = null;
        }

        
        // scrolling up
        if (eventData.scrollDelta.y > 0)
        {
            ScrollToNextItem();
        }
        // scrolling down
        else if (eventData.scrollDelta.y < 0)
        {
            ScrollToPreviousItem();
        }
    }

    private void ScrollToPreviousItem()
    {
        // cant scroll past the top
        if (contentPanel.anchoredPosition.y < _maxScrollPosition)
        {
            contentPanel.anchoredPosition += new Vector2(0, _scrollAmount);
        }
    }

    private void ScrollToNextItem()
    {
        // cant scroll past the bottom
        if (contentPanel.anchoredPosition.y > _minScrollPosition)
        {
            contentPanel.anchoredPosition -= new Vector2(0, _scrollAmount);
        }
    }
    
    public void CenterButton(Button button)
    {
        GameObject currentClickedButton = button.gameObject;

        if (clickedButton == currentClickedButton) 
        {
            // The same button was clicked again, so craft the item
            recipeLoader.CraftItem(currentClickedButton);
        }
        else 
        {
            // A different button was clicked
            if (_isScrolling) return;

            clickedButton = currentClickedButton;
            if (_previousButton != null) 
            {
                recipeLoader.HideMaterials(_previousButton);
            }

            // Start scrolling
            StartCoroutine(CenterButtonCoroutine(button));
        }
    }


    private IEnumerator CenterButtonCoroutine(Button button)
    {
        _isScrolling = true;
        
        RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
        CanvasScaler canvasScaler = FindCanvasScaler(buttonRectTransform);
        float screenCenterY = Screen.height / 2;
        float scrollSpeed = 10f; // scroll speed
        
        float scaleFactor = canvasScaler != null ? canvasScaler.scaleFactor : 1f;

        while (true)
        {
            Vector2 buttonScreenPosition = RectTransformUtility.WorldToScreenPoint(null, buttonRectTransform.position);
            float distanceToCenter = screenCenterY - buttonScreenPosition.y;
            float direction = distanceToCenter > 0 ? 1 : -1;
            float step = direction * Mathf.Min(Mathf.Abs(distanceToCenter), scrollSpeed); // use the smaller of the two values to avoid overshooting

            contentPanel.anchoredPosition += new Vector2(0, step);

            if (Mathf.Abs(distanceToCenter) < 15f / scaleFactor) // Use a scaled threshold to determine when to stop
            {
                // animation ended
                if (recipeLoader != null) recipeLoader.ShowMaterials(button.gameObject);

                var o = button.gameObject;
                _previousButton = o;
                _currentButton = o;
                
                _isScrolling = false;
                
                break;
            }

            yield return null;
        }
    }
    
    private CanvasScaler FindCanvasScaler(Component component)
    {
        // Search up the hierarchy for a CanvasScaler component
        Canvas canvas = component.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            return canvas.GetComponent<CanvasScaler>();
        }
        return null;
    }
}