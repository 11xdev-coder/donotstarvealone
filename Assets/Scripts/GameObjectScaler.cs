using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameObjectScaler : MonoBehaviour
{
    public GameObject tileIndicator;

    void Start()
    {
        AdjustTileIndicatorSize();
    }

    void AdjustTileIndicatorSize()
    {
        if (tileIndicator == null) return;

        // Reference sizes for different resolutions
        Vector3 size5x4 = new Vector3(8f, 8f, 1f); // Ideal size for 5x4 (1280x1024)
        Vector3 size4K = new Vector3(3f, 3f, 1f);  // Ideal size for 4K (3840x2160)

        // Current screen resolution
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Calculate the relative size based on the screen width
        float relativeWidth = screenWidth / 3840f; 

        // Interpolating size between 5x4 and 4K based on relative width
        float sizeFactor = Mathf.Lerp(size5x4.x, size4K.x, relativeWidth);

        // Apply the calculated size to the TileIndicator
        Vector3 newSize = new Vector3(sizeFactor, sizeFactor, 1f);
        tileIndicator.transform.localScale = newSize;
    }
}
