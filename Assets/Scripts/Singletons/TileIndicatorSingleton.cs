using UnityEngine;

namespace Singletons
{
    public class TileIndicatorSingleton : MonoBehaviour
    {
        public static GameObject Instance { get; private set; }
    
        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of TileIndicator found!");
                return;
            }
        
            Instance = gameObject;
        }
    }
}
