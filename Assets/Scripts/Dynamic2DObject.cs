using UnityEngine;

public class Dynamic2DObject : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    public int baseSortingOrder;
    public int orderOffset;
    public bool doSort;
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

            transform.eulerAngles = new Vector3(_camera.transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);

            UpdateSortingLayers();
        }
        else if (isActive)
        {
            DeactivateObject();
            isActive = false;
        }
    }

    private bool IsInView()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        return GeometryUtility.TestPlanesAABB(planes, GetComponent<Collider2D>().bounds);
    }

    private void ActivateObject()
    {
        // Enable components or behaviors as needed
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
    }

    private void DeactivateObject()
    {
        // Disable components or behaviors as needed
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
