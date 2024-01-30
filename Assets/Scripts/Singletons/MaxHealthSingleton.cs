using TMPro;
using UnityEngine;

namespace Singletons
{
    public class MaxHealthSingleton : MonoBehaviour
    {
        public static TMP_Text Instance { get; private set; }
    
        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of MaxHealth found!");
                return;
            }
        
            Instance = GetComponent<TMP_Text>();
        }
    }
}
