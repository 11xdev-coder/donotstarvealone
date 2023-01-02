using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worldgenerator : MonoBehaviour
{
    [Header("world settings")]
    public int width = 1000;
    public int height = 1000;
    public int freq = 2;
    
    
    [Header("tile prefabs")]
    public GameObject grassTurf;
    public GameObject forestTurf;
    public GameObject rockylandTurf;
    public GameObject ocean;

    float[,] noiseValues;
    // Start is called before the first frame update
    void Start()
    {
        noiseValues = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                noiseValues[x, y] = Mathf.PerlinNoise((float)x / width * freq, (float)y / height * freq);
            }
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = noiseValues[x, y];
                GameObject terrain;

                if (noiseValue > 0.5f)
                {
                    if (noiseValue > 0.6f)
                    {
                        if (noiseValue >= 0.7f)
                        {
                            terrain = rockylandTurf;
                        }
                        else
                        {
                            terrain = forestTurf;
                        }
                    }
                    else
                    {
                        terrain = grassTurf;
                    }
                }
                else
                {
                    terrain = ocean;
                }

                Vector3 position = new Vector3(x, y, 0);
                Instantiate(terrain, position, Quaternion.identity);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
