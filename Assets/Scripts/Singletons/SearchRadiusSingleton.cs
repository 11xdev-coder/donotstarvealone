using UnityEngine;
using UnityEngine.UI;

namespace Singletons
{
    public class SearchRadiusSingleton : MonoBehaviour
    {
        public static Button Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of SearchRadiusSingleton found!");
                return;
            }
    
            Instance = GetComponent<Button>();
        }
    }
}
