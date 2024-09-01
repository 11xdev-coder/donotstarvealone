using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class KeyBindingManager : MonoBehaviour
{
    [System.Serializable] // Makes it editable in the inspector
    public class KeybindingObject
    {
        public string actionName; // MoveForward, Jump, etc.
        public string tag;
    }
    
    public static KeyBindingManager Instance { get; private set; }

    [Header("Bindings")]
    public ControlBindings bindings; // Assign this in the inspector
    
    [Header("Main")]
    public bool isWaitingForKeyPress;
    private KeyCode m_CurrentKey;

    [Header("UI")] 
    private TMP_Text _waitingForKeyText;
    public List<KeybindingObject> keybindings; // List of KeybindingObjects

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Multiple instances of KeyBindingManager found!");
            return;
        }

        Instance = this;

        _waitingForKeyText = GameObject.FindGameObjectWithTag("KeybidingWaitingText").GetComponent<TMP_Text>();
        _waitingForKeyText.gameObject.SetActive(false);
    }

    private void Start()
    {
        foreach (var binding in keybindings)
        {
            string localAction = binding.actionName;
            Button localBtn = GameObject.FindGameObjectWithTag(binding.tag).GetComponent<Button>();
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

        Instance = this;
    }
    
    #region rebinding
    void StartRebindingKey(string keyName, Button buttonToUpdate)
    {
        if (!isWaitingForKeyPress)
        {
            isWaitingForKeyPress = true;
            _waitingForKeyText.gameObject.SetActive(true);
            StartCoroutine(FadeIn(_waitingForKeyText.GetComponent<CanvasGroup>()));
            StartCoroutine(WaitForKeyPress(keyName, buttonToUpdate));
        }
    }

    IEnumerator WaitForKeyPress(string keyName, Button buttonToUpdate)
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
        StartCoroutine(FadeOut(_waitingForKeyText.GetComponent<CanvasGroup>()));
        UpdateButtonLabels();
    }

    void UpdateButtonLabels()
    {
        foreach (var binding in keybindings)
        {
            string action = binding.actionName;
            Button btn = GameObject.FindGameObjectWithTag(binding.tag).GetComponent<Button>();
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
            canvasGroup.alpha += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f; 
    }

    IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        // Fade Out
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;

        canvasGroup.gameObject.SetActive(false);
    }
    
    #endregion
}
