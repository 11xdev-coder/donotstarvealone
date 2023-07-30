using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowUIText : MonoBehaviour
{
    public GameObject text;
    
    // USE THIS FOR EVENT TRIGGERS
    private void Start()
    {
        text.SetActive(false);
    }

    public bool IsMouseOverUI()
    {
        if (Vector2.Distance(transform.position, Input.mousePosition) <= 65) return true;
        return false;
    }

    private void Update()
    {
        if (IsMouseOverUI()) text.SetActive(true);
        else text.SetActive(false);
    }
}
