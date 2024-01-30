using UnityEngine;
using UnityEngine.UI;

namespace Singletons
{
    public class ColorImageSingleton : MonoBehaviour
    {
        public static Image Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of ColorImageSingleton found!");
                return;
            }
    
            Instance = GetComponent<Image>();
        }
    }
}
