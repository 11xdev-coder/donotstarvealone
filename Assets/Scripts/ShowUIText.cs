using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowUIText : NetworkBehaviour
{
    public bool found;
    public GameObject text;
    
    public void Awake()
    {
       text.SetActive(true);
    }

    private void Update()
    {
        if(found) text.SetActive(GetComponent<PointerCheck>().IsMouseOver());
    }
}
