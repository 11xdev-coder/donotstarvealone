using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public GameObject target;
    public Vector3 offset;
	
    // Update is called once per frame
    void FixedUpdate () {
        if (target)
        {
            this.transform.position = target.transform.position + offset;
        }
    }
}
