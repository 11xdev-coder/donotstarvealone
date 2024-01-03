using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowUIText : MonoBehaviour
{
    public GameObject text;
    
    private void Start()
    {
        text.SetActive(false);
    }

    private void Update()
    {
        text.SetActive(GetComponent<PointerCheck>().IsMouseOver());
    }
}
