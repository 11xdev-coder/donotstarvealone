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
    
    private Camera _camera;
    // Start is called before the first frame update=
    void Start()
    {
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
        if(doSort) _spriteRenderer.sortingOrder = baseSortingOrder - Mathf.FloorToInt(transform.position.y) + orderOffset;
        transform.eulerAngles = new Vector3(_camera.transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
    }
}
