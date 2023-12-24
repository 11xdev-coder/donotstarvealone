using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeLoader : MonoBehaviour
{
    [Header("Main")]
    public GameObject recipeButtonPrefab;
    public GameObject materialButtonPrefab;
    public Transform contentPanel; // scroll rect content
    public RecipeScroller recipeScroller;
    public GameObject craftingMenuPanel;
    public PlayerController player;
    
    [Header("Animation")]
    [SerializeField] private float moveAmount;
    [SerializeField] private float animationDuration = 0.5f; 
    private bool _isAnimating;
    private bool _isOpen;

    [Header("Material Animation")]
    [SerializeField] private float startSpacing;
    [SerializeField] private float materialOpenAnimationDuration;
    [SerializeField] private float fadeInSpeed;
    [SerializeField] private float fadeOutSpeed;
    
    private List<CraftingRecipeClass> _craftingRecipes = new List<CraftingRecipeClass>();
    private Coroutine _currentFadeCoroutine;
    private GameObject _currentlyHidingButton;
    private GameObject _buttonWithShownMaterials;

    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
        LoadRecipes();
        CreateButtons();
    }
    
    public void OpenMenu()
    {
        if (_isAnimating) return;

        float targetMoveAmount = _isOpen ? -moveAmount : moveAmount;
        StartCoroutine(SlidePanel(targetMoveAmount));
        _isOpen = !_isOpen;
        recipeScroller.clickedButton = null;
        
        if (!_isOpen && _buttonWithShownMaterials != null)
        {
            if (_currentFadeCoroutine != null)
            {
                StopCoroutine(_currentFadeCoroutine);
            }
            HideMaterials(_buttonWithShownMaterials);
        }
    }

    private IEnumerator SlidePanel(float targetX)
    {
        _isAnimating = true;
        Vector3 startPosition = craftingMenuPanel.transform.localPosition;
        Vector3 endPosition = startPosition + new Vector3(targetX, 0, 0);

        float timeElapsed = 0;

        while (timeElapsed < animationDuration)
        {
            float t = timeElapsed / animationDuration;
            t = t * t * (3f - 2f * t); // ease out

            craftingMenuPanel.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
            timeElapsed += Time.deltaTime;
            yield return null; // wait for the next frame
        }

        craftingMenuPanel.transform.localPosition = endPosition;
        _isAnimating = false;
    }

    private void LoadRecipes()
    {
        _craftingRecipes.Clear();
        Object[] recipes = Resources.LoadAll("Recipes", typeof(CraftingRecipeClass));
        foreach (var recipe in recipes)
        {
            _craftingRecipes.Add((CraftingRecipeClass)recipe); // convert recipe to CraftingRecipeClass and add it
        }
    }
    
    private void CreateButtons()
    {
        foreach (var recipe in _craftingRecipes)
        {
            // recipe result button
            GameObject resultButtonObject = Instantiate(recipeButtonPrefab, contentPanel);
            resultButtonObject.transform.Find("Icon").GetComponent<Image>().sprite = recipe.outputItem.item.itemSprite;
            // resultButtonObject.GetComponent<Button>().onClick.AddListener(() => CraftItem(recipe));
            resultButtonObject.GetComponent<Button>().onClick.AddListener(() => recipeScroller.CenterButton(resultButtonObject.GetComponent<Button>()));
            resultButtonObject.name = recipe.outputItem.item.itemName + " Recipe";

            Transform materialsTransform = resultButtonObject.transform.Find("Materials");

            // create materials
            foreach (var material in recipe.inputItems)
            {
                GameObject materialButtonObject = Instantiate(materialButtonPrefab, materialsTransform);
                materialButtonObject.transform.Find("Icon").GetComponent<Image>().sprite = material.item.itemSprite; 
                materialButtonObject.transform.Find("Count").GetComponent<Text>().text = material.count > 1 ? material.count.ToString() : " ";

                // position the button
                RectTransform rectTransform = materialButtonObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x + rectTransform.rect.width, rectTransform.anchoredPosition.y);
                materialButtonObject.name = material.item.itemName + " Material";
            }
            
            materialsTransform.gameObject.SetActive(false);
        }
    }

    public void ShowMaterials(GameObject button)
    {
        if (_isAnimating) return;
        
        button.transform.Find("Materials").gameObject.SetActive(true);
        
        StartCoroutine(MaterialOpenAnimationCoroutine(button));
        CanvasGroup canvasGroup = button.transform.Find("Materials").GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        _currentFadeCoroutine = StartCoroutine(FadeIn(canvasGroup));
        _buttonWithShownMaterials = button;
    }

    public void HideMaterials(GameObject button)
    {
        if (button == recipeScroller.clickedButton && !_isAnimating) return;
        
        if (_currentFadeCoroutine != null && _currentlyHidingButton != null)
        {
            StopCoroutine(_currentFadeCoroutine);
            HideMaterialsInstantly(_currentlyHidingButton);
            _currentlyHidingButton = null;
        }
        
        CanvasGroup canvasGroup = button.transform.Find("Materials").GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        _currentFadeCoroutine = StartCoroutine(FadeOut(canvasGroup)); // dont need to SetActive(false) since we are doing it in FadeOut
        _currentlyHidingButton = button;
    }

    public void HideMaterialsInstantly(GameObject button)
    {
        var canvasGroup = button.transform.Find("Materials").GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }
    
    private IEnumerator MaterialOpenAnimationCoroutine(GameObject button)
    {
        HorizontalLayoutGroup group = button.transform.Find("Materials").GetComponent<HorizontalLayoutGroup>();
        float initialSpacing = group.spacing; // initial spacing

        float timeElapsed = 0;

        while (timeElapsed < materialOpenAnimationDuration)
        {
            float t = timeElapsed / materialOpenAnimationDuration;
            t = t * t * (3f - 2f * t); // ease out

            group.spacing = Mathf.Lerp(startSpacing, initialSpacing, t);
            timeElapsed += Time.deltaTime;
            yield return null; // wait for the next frame
        }

        group.spacing = initialSpacing;
    }

    private IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        // from 0 to 1
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeInSpeed; // speed
            yield return null;
        }
        canvasGroup.alpha = 1f;
        _currentFadeCoroutine = null;
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        // from 1 to 0
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeOutSpeed; // speed
            yield return null;
        }
        canvasGroup.alpha = 0f; 

        canvasGroup.gameObject.SetActive(false);
        _currentFadeCoroutine = null;
    }
    
    public void CraftItem(GameObject button)
    {
        // Find the recipe associated with the button
        string recipeName = button.name.Replace(" Recipe", "");
        CraftingRecipeClass recipeToCraft = _craftingRecipes.Find(r => r.outputItem.item.itemName == recipeName);

        if (recipeToCraft != null)
        {
            player.Craft(recipeToCraft);
        }
    }
}