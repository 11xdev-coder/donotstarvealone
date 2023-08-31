using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour
{
    public TMP_InputField codeInputField;
    public TMP_Text consoleOutput;
    public Button executeButton;
    public GameObject thisMenu;

    private ScrollRect scrollRect;

    private void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        executeButton.onClick.AddListener(() => ExecuteCommand(codeInputField.text));
    }

    void ExecuteCommand(string command)
    {
        string[] parts = command.Split(new char[] { ' ' }, 2);
        string cmd = parts[0];

        switch (cmd)
        {
            case "c_spawn":
                if (parts.Length > 1)
                {
                    SpawnItem(parts[1]);
                }
                else
                {
                    AddConsoleOutputText("Usage: c_spawn <item> {<componentname>:<componentvalue>=<newvalue>}");
                }
                break;

            default:
                AddConsoleOutputText($"Unknown command: {cmd}");
                break;
        }
    }

    void SpawnItem(string commandArgs)
    {
        // Splitting itemName and modifications
        string itemName = commandArgs.Split('{')[0].Trim();
        string modifications = commandArgs.Contains('{') ? commandArgs.Split('{', '}')[1].Trim() : "";

        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");

        GameObject itemPrefab = null;

        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab.name == itemName)
            {
                itemPrefab = prefab;
                break;
            }
        }

        if (itemPrefab)
        {
            Vector3 screenPos = Input.mousePosition;
            screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
            mousePos.z = 0;

            GameObject instance = Instantiate(itemPrefab, mousePos, Quaternion.identity);

            if (!string.IsNullOrEmpty(modifications))
            {
                SetComponentProperties(instance, modifications);
            }

            AddConsoleOutputText($"Spawned {itemName}.");
        }
        else
        {
            AddConsoleOutputText($"Item '{itemName}' not found.");
        }
    }

    private void SetComponentProperties(GameObject obj, string modifications)
    {
        // Split the modifications string by semicolon to get individual modifications
        string[] modificationList = modifications.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string modification in modificationList)
        {
            SetSingleComponentProperty(obj, modification.Trim());
        }
    }

    private void SetSingleComponentProperty(GameObject obj, string modification)
    {
        string[] componentParts = modification.Split(new char[] { ':', '=' }, StringSplitOptions.RemoveEmptyEntries);
        string componentName = componentParts[0].Trim();
        string propertyName = componentParts[1].Trim();
        string propertyValue = componentParts[2].Trim();

        Component component = obj.GetComponent(componentName);

        if (component == null)
        {
            AddConsoleOutputText($"Object does not have component '{componentName}'.");
            return;
        }

        PropertyInfo propertyInfo = component.GetType().GetProperty(propertyName);
        FieldInfo fieldInfo = component.GetType().GetField(propertyName);

        if (propertyInfo == null && fieldInfo == null)
        {
            AddConsoleOutputText($"Property or Field '{propertyName}' not found in component '{componentName}'.");
            return;
        }

        try
        {
            if (propertyInfo != null)
            {
                if (propertyInfo.PropertyType == typeof(ItemClass))
                {
                    ItemClass itemObj = GetItemFromScriptableObject(propertyValue);
                    if (itemObj)
                    {
                        propertyInfo.SetValue(component, itemObj);
                    }
                }
                else
                {
                    SetValueUsingReflection(component, propertyInfo.PropertyType, propertyInfo.SetValue, propertyValue);
                }
            }
            else if (fieldInfo != null)
            {
                if (fieldInfo.FieldType == typeof(ItemClass))
                {
                    ItemClass itemObj = GetItemFromScriptableObject(propertyValue);
                    if (itemObj)
                    {
                        fieldInfo.SetValue(component, itemObj);
                    }
                }
                else
                {
                    SetValueUsingReflection(component, fieldInfo.FieldType, fieldInfo.SetValue, propertyValue);
                }
            }

        }
        catch (Exception ex)
        {
            AddConsoleOutputText($"Failed to set value. Error: {ex.Message}");
        }
    }
    
    private void SetValueUsingReflection(object component, Type type, Action<object, object> setValue, string stringValue)
    {
        if (type == typeof(Vector3))
        {
            Vector3 vecResult;
            if (TryParseVector3(stringValue, out vecResult))
            {
                setValue(component, vecResult);
            }
            else
            {
                AddConsoleOutputText($"Failed to parse Vector3 value.");
            }
        }
        else if (type == typeof(int))
        {
            setValue(component, Int32.Parse(stringValue));
        }
        else if (type == typeof(bool))
        {
            setValue(component, bool.Parse(stringValue));
        }
        else if (type == typeof(float))
        {
            setValue(component, float.Parse(stringValue));
        }
    }

    private void AddConsoleOutputText(string text)
    {
        consoleOutput.text += "\n" + text;

        // Ensure the canvas updates and scroll to the bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;  // This scrolls to the bottom
    }

    ItemClass GetItemFromScriptableObject(string itemName)
    {
        if(itemName.StartsWith("Item"))
        {
            itemName = itemName.Substring(4); // Remove "Item" prefix
        }

        itemName = itemName.Replace('_', ' '); // Replace underscores with spaces

        // Load the ScriptableObject using its name from Resources/Items directory
        ScriptableObject scriptableObj = Resources.Load<ScriptableObject>("Items/" + itemName);

        if (scriptableObj is ItemClass itemClassInstance)
        {
            return itemClassInstance.GetItem();
        }
        else
        {
            AddConsoleOutputText($"Item '{itemName}' not found or is not of correct type.");
            return null;
        }
    }

    private bool TryParseVector3(string value, out Vector3 result)
    {
        string[] parts = value.Split(',');

        if (parts.Length == 3)
        {
            float x, y, z;
            if (float.TryParse(parts[0].Trim(), out x) && 
                float.TryParse(parts[1].Trim(), out y) && 
                float.TryParse(parts[2].Trim(), out z))
            {
                result = new Vector3(x, y, z);
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }


}
