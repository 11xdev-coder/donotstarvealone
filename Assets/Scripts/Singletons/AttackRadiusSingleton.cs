using UnityEngine;
using UnityEngine.UI;

namespace Singletons
{
    public class AttackRadiusSingleton : MonoBehaviour
    {
        public static Button Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of AttackRadiusSingleton found!");
                return;
            }
    
            Instance = GetComponent<Button>();
        }
    }
}
