using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dynamic2DObject : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;

    // The base sorting order, which can be adjusted if needed.
    public int baseSortingOrder;
    public int orderOffset;
    public bool doSort;
    public GameObject player;
    
    private Camera _camera;
    // Start is called before the first frame update=
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (doSort)
        {
            baseSortingOrder = FindObjectOfType<WorldGenerator>().height;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        _camera = FindObjectOfType<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (doSort)
        {
            _spriteRenderer.sortingOrder = baseSortingOrder - Mathf.FloorToInt(transform.position.y) + orderOffset;
        }

        transform.eulerAngles = new Vector3(_camera.transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);

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
