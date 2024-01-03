using UnityEngine;

public class Dynamic2DObject : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    
    [Header("Ordering")]
    public int baseSortingOrder;
    public int orderOffset;
    public bool doSort;

    [Header("Misc")] 
    public bool deactivate;
    public bool doColliderCheck;
    
    [Header("Invertions")]
    [Tooltip("For particles to keep them looking straight (z rotation must be -90)")]
    public bool invertRotationForParticle;
    public bool invertXRotation;
    
    private GameObject player;
    private Camera _camera;
    private bool isActive = true;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (doSort)
        {
            baseSortingOrder = FindObjectOfType<WorldGenerator>().height;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        _camera = Camera.main;
    }

    void Update()
    {
        // Check if the object is within the camera's view
        if (IsInView())
        {
            if (!isActive)
            {
                ActivateObject();
                isActive = true;
            }

            if (doSort)
            {
                _spriteRenderer.sortingOrder = baseSortingOrder - Mathf.FloorToInt(transform.position.y) + orderOffset;
            }
            
            var eulerAngles = transform.eulerAngles;
            var cameraEulerAngles = _camera.transform.eulerAngles;
            
            if (invertRotationForParticle)
            {
                eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, -90 - cameraEulerAngles.x + cameraEulerAngles.x);
            }
            else if (invertXRotation)
            {
                eulerAngles = new Vector3(-90 - cameraEulerAngles.x + cameraEulerAngles.x, eulerAngles.y, eulerAngles.z);
            }
            else
            {
                eulerAngles = new Vector3(cameraEulerAngles.x, eulerAngles.y, eulerAngles.z);
            }
            
            transform.eulerAngles = eulerAngles;
            

            UpdateSortingLayers();
        }
        else if (isActive)
        {
            DeactivateObject();
            isActive = false;
        }
    }

    public bool IsInView()
    {
        if (doColliderCheck)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
            return GeometryUtility.TestPlanesAABB(planes, GetComponent<Collider2D>().bounds);
        }

        return true;
    }

    private void ActivateObject()
    {
        if(deactivate) gameObject.SetActive(true);
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
    }

    private void DeactivateObject()
    {
        if (deactivate) gameObject.SetActive(false);
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;
    }

    private void UpdateSortingLayers()
    {
        // Get all colliders attached to this object and the player
        Collider2D[] objectColliders = GetComponents<Collider2D>();
        Collider2D[] playerColliders = player.GetComponents<Collider2D>();

        Collider2D objectCollider = DefineCollider(objectColliders);
        Collider2D playerCollider = DefineCollider(playerColliders);

        // If we found the non-trigger colliders for both the player and the object
        if (playerCollider != null && objectCollider != null && _spriteRenderer != null)
        {
            float playerBottom = playerCollider.bounds.min.y;
            float objectTop = objectCollider.bounds.max.y;
            if (playerBottom > objectTop)
            {
                _spriteRenderer.sortingLayerName = "ObjectInFront";
            }
            else
            {
                _spriteRenderer.sortingLayerName = "ObjectBehind";
            }
        }
    }

    private Collider2D DefineCollider(Collider2D[] colliders)
    {
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
            {
                return col;
            }
        }

        return null;
    }
}
