using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class KeyBindingManager : MonoBehaviour
{
    [System.Serializable] // Makes it editable in the inspector
    public class KeybindingObject
    {
        public string actionName; // MoveForward, Jump, etc.
        public Button keybindingButton;
    }
    
    public KeyBindingManager instance { get; private set; }

    [Header("Bindings")]
    public ControlBindings bindings; // Assign this in the inspector
    
    [Header("Main")]
    public bool isWaitingForKeyPress;
    private KeyCode m_CurrentKey;

    [Header("UI")] 
    public TMP_Text waitingForKeyText;
    public List<KeybindingObject> keybindings; // List of KeybindingObjects

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // This makes sure the object persists across scene loads
        }
        else
        {
            Destroy(gameObject); // Ensures that no more than one instance can exist at any time
        }
        
        DontDestroyOnLoad(waitingForKeyText);
        waitingForKeyText.gameObject.SetActive(false);
    }

    private void Start()
    {
        foreach (var binding in keybindings)
        {
            string localAction = binding.actionName;
            Button localBtn = binding.keybindingButton;
            localBtn.GetComponent<Button>().onClick.AddListener(() => StartRebindingKey(localAction, localBtn));
        }
        UpdateButtonLabels();
    }

    private void Update()
    {
        if (isWaitingForKeyPress)
        {
            foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyUp(vKey) && vKey != KeyCode.Mouse0) // avoid bugs
                {
                    m_CurrentKey = vKey;
                    isWaitingForKeyPress = false;
                    break;
                }
            }
        }
    }
    
    #region rebinding
    void StartRebindingKey(string keyName, Button buttonToUpdate)
    {
        if (!isWaitingForKeyPress)
        {
            isWaitingForKeyPress = true;
            waitingForKeyText.gameObject.SetActive(true);
            StartCoroutine(FadeIn(waitingForKeyText.GetComponent<CanvasGroup>()));
            StartCoroutine(WaitForKeyPress(keyName, buttonToUpdate));
        }
    }

    System.Collections.IEnumerator WaitForKeyPress(string keyName, Button buttonToUpdate)
    {
        while (isWaitingForKeyPress)
        {
            yield return null;
        }
        // Set the key binding in the ScriptableObject
        System.Reflection.FieldInfo field = bindings.GetType().GetField(keyName);
        if (field != null)
        {
            field.SetValue(bindings, m_CurrentKey);
        }
        StartCoroutine(FadeOut(waitingForKeyText.GetComponent<CanvasGroup>()));
        UpdateButtonLabels();
    }

    void UpdateButtonLabels()
    {
        foreach (var binding in keybindings)
        {
            string action = binding.actionName;
            Button btn = binding.keybindingButton;
            System.Reflection.FieldInfo field = bindings.GetType().GetField(action);
            if (field != null)
            {
                KeyCode boundKey = (KeyCode)field.GetValue(bindings);
                btn.GetComponentInChildren<TMP_Text>().text = $"{action}: {boundKey.ToString()}";
            }
        }
    }
    #endregion
    
    #region Text
    
    IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        // Fade In
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime; // Adjust this value to control the fade speed
            yield return null;
        }
        canvasGroup.alpha = 1f;  // Ensure it's completely opaque
    }

    IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        // Fade Out
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime; // Adjust this value to control the fade speed
            yield return null;
        }
        canvasGroup.alpha = 0f;  // Ensure it's completely transparent

        canvasGroup.gameObject.SetActive(false);  // Disable the object to prevent it from blocking other UI
    }
    
    #endregion
}
