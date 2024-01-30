using UnityEngine;
using UnityEngine.UI;

namespace Singletons
{
    public class ItemCursorSingleton : MonoBehaviour
    {
        public static GameObject Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of ItemCursorSingleton found!");
                return;
            }
    
            Instance = gameObject;
        }
    }
}
