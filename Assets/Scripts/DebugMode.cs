using Mirror;
using Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DebugMode : NetworkBehaviour
{
    [Header("BUTTONS")] 
    public Button searchRadius;
    public Button attackDetectionRadius;

    [Header("Main")] 
    public bool showSearchRadius;
    public bool showAttackDetectionRadius;
    public bool canAccessDebug;
    private GameObject _menu;
    private KeyBindingManager _keyBindingManager;
    
    [Header("-- Debug Rendering --")]
    public LineRenderer attackDetectionRadiusRenderer;
    public LineRenderer searchRadiusRenderer;
    [Tooltip("Smoothness of a circle")]
    public int segments = 50;

    [Header("-- Assignable - Important --")]
    public PlayerController pc;
    public LineRenderer lrPrefab;
    public Image color;

    public override void OnStartClient()
    {
        searchRadius = SearchRadiusSingleton.Instance;
        attackDetectionRadius = AttackRadiusSingleton.Instance;
        _menu = GameObject.FindGameObjectWithTag("DebugMenu");
        _keyBindingManager = KeyBindingManager.Instance;
        color = ColorImageSingleton.Instance;
        
        // Instantiate the LineRenderers
        var position = transform.position;
        attackDetectionRadiusRenderer = InstantiateLineRendererInactive(lrPrefab.gameObject, position);
        searchRadiusRenderer = InstantiateLineRendererInactive(lrPrefab.gameObject, position);

        SetupLineRenderer(attackDetectionRadiusRenderer, pc.playerAttackDetectionRadius);
        SetupLineRenderer(searchRadiusRenderer, pc.searchRadius);
        
        UpdateLabels();
        
        searchRadius.GetComponent<Button>().onClick.AddListener(() => ToggleProperty("searchRadius"));
        attackDetectionRadius.GetComponent<Button>().onClick.AddListener(() => ToggleProperty("attackDetectionRadius"));
    }

    private void Update()
    {
        var position = pc.GetComponent<Rigidbody2D>().position;
        
        if(searchRadius == null) searchRadius = SearchRadiusSingleton.Instance;
        if (attackDetectionRadius == null) attackDetectionRadius = AttackRadiusSingleton.Instance;
        if(color == null) color = ColorImageSingleton.Instance;
        if(_keyBindingManager == null)  _keyBindingManager = KeyBindingManager.Instance;
        
        if (searchRadiusRenderer == null) // try to init it again if null
        {
            searchRadiusRenderer = InstantiateLineRendererInactive(lrPrefab.gameObject, position);
            SetupLineRenderer(searchRadiusRenderer, pc.searchRadius);
        }
        
        if (attackDetectionRadiusRenderer == null)
        {
            attackDetectionRadiusRenderer = InstantiateLineRendererInactive(lrPrefab.gameObject, position);
            SetupLineRenderer(attackDetectionRadiusRenderer, pc.playerAttackDetectionRadius);
        }
        
        
        if (showSearchRadius) // show search radius
        {
            searchRadiusRenderer.transform.position = position;
        
            // Activate LR if not active
            if (!searchRadiusRenderer.gameObject.activeSelf)
            {
                searchRadiusRenderer.gameObject.SetActive(true);
            }
        }
        else
        {
            // Deactivate LR if active
            if (searchRadiusRenderer.gameObject.activeSelf)
            {
                searchRadiusRenderer.gameObject.SetActive(false);
            }
        }

        if (showAttackDetectionRadius) // show attack radius
        {
            attackDetectionRadiusRenderer.transform.position = position;
            
            // Activate LR if not active
            if (!attackDetectionRadiusRenderer.gameObject.activeSelf)
            {
                attackDetectionRadiusRenderer.gameObject.SetActive(true);
            }
        }
        else
        {
            // Deactivate LR if active
            if (attackDetectionRadiusRenderer.gameObject.activeSelf)
            {
                attackDetectionRadiusRenderer.gameObject.SetActive(false);
            }
        }

        attackDetectionRadiusRenderer.startColor = color.color;
        attackDetectionRadiusRenderer.endColor = color.color;

        searchRadiusRenderer.startColor = color.color;
        searchRadiusRenderer.endColor = color.color;
       
        // TODO: _keyBindingManager is null
        // if not setting a key, pressed debug key and can access debug menu
        if (!_keyBindingManager.isWaitingForKeyPress && 
            Input.GetKeyUp(_keyBindingManager.bindings.OpenDebugMenu) && canAccessDebug && !pc.console.activeSelf)
        {
            switch (_menu.activeSelf)
            {
                case true:
                    _menu.SetActive(false);
                    break;
                case false:
                    _menu.SetActive(true);
                    break;
            }
        }
    }
    
    // This helper function instantiates the LineRenderer but sets it as inactive.
    private LineRenderer InstantiateLineRendererInactive(GameObject prefab, Vector3 position)
    {
        var lrInstance = Instantiate(prefab, position, Quaternion.identity);
        lrInstance.SetActive(false); // Setting the GameObject as inactive
        return lrInstance.GetComponent<LineRenderer>();
    }
    
    private void ToggleProperty(string property)
    {
        switch (property)
        {
            case "searchRadius":
                showSearchRadius = !showSearchRadius;
                break;
            case "attackDetectionRadius":
                showAttackDetectionRadius = !showAttackDetectionRadius;
                break;
        }
        
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        searchRadius.GetComponentInChildren<TMP_Text>().text = "Show player's search radius : " + (showSearchRadius ? "True" : "False");
        attackDetectionRadius.GetComponentInChildren<TMP_Text>().text = "Show player's attack detection radius : " + (showAttackDetectionRadius ? "True" : "False");
    }
    
    void SetupLineRenderer(LineRenderer lineRenderer, float radius)
    {
        lineRenderer.positionCount = segments + 1;
        float angle = 20f;
        for (int i = 0; i < (segments + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
        
            lineRenderer.SetPosition(i, new Vector3(x, y, 0) + pc.attackDetectionOffset);
            angle += (360f / segments);
        }
    }
}
