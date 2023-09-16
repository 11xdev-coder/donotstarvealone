using System;
using System.Collections;
using System.Collections.Generic;
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
    private PlayerController nearestPlayer;

    private void Start()
    {
        nearestPlayer = FindObjectOfType<PlayerController>();
        scrollRect = GetComponent<ScrollRect>();
        executeButton.onClick.AddListener(() => ExecuteCommand(codeInputField.text));
        codeInputField.onEndEdit.AddListener((text) => ExecuteCommand(codeInputField.text));
    }

    void ExecuteCommand(string input)
{
    string[] parts = input.Split(new[] { ' ', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
    string command = parts[0];

    switch (command)
    {
        case "c_give":
            if (parts.Length >= 3)
            {
                string itemName = parts[1];
                int itemCount = Int32.Parse(parts[2]);
                Dictionary<string, string> properties = new Dictionary<string, string>();

                if (parts.Length > 3)
                {
                    // Assuming properties are provided in {property1=value1;property2=value2} format
                    string propertiesString = parts[3].TrimStart('{').TrimEnd('}');
                    string[] propertyPairs = propertiesString.Split(';');
                    foreach (string pair in propertyPairs)
                    {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length == 2)
                        {
                            properties[keyValue[0]] = keyValue[1]; // Store property-value pairs
                        }
                    }
                }

                GiveItemCommand(itemName, itemCount, properties);
            }

            break;

        case "c_spawn":
            if (!string.IsNullOrEmpty(input.Substring(command.Length).Trim()))
            {
                SpawnItem(input.Substring(command.Length).Trim());
            }
            else
            {
                AddConsoleOutputText("Usage: c_spawn <item> {<componentname>:<componentvalue>=<newvalue>}");
            }

            break;

        default:
            AddConsoleOutputText($"Unknown command: {command}");
            break;
    }
}

    #region Giving
    
    void GiveItemCommand(string itemReference, int count, Dictionary<string, string> propertyTags = null)
    {
        ItemClass item = GetItemFromScriptableObject(itemReference);

        if (item != null)
        {
            // Modify properties based on provided tags
            if (propertyTags != null)
            {
                foreach (var tag in propertyTags)
                {
                    ModifyItemProperty(item, tag.Key, tag.Value);
                }
            }

            // Assuming you have a player inventory instance available:
            nearestPlayer.inventory.Add(item, count);

            AddConsoleOutputText($"Given {item.itemName} to the player.");
        }
        else
        {
            AddConsoleOutputText($"Failed to give item: {itemReference} not found.");
        }
    }

    void ModifyItemProperty(ItemClass item, string propertyName, string propertyValue)
    {
        // Attempt to get the property
        var property = item.GetType().GetProperty(propertyName);

        if (property != null)
        {
            // For now, assuming all properties you're going to modify are of type int for simplicity
            property.SetValue(item, ConvertValueToType(property.PropertyType, propertyValue));
        }
        else
        {
            // If the property was not found, attempt to get the field
            var field = item.GetType().GetField(propertyName);

            if (field != null)
            {
                field.SetValue(item, ConvertValueToType(field.FieldType, propertyValue));
            }
            else
            {
                AddConsoleOutputText($"Property/Field {propertyName} not found on item.");
            }
        }
    }
    
    ItemClass GetItemFromScriptableObject(string itemName)
    {
        if (itemName.StartsWith("Item"))
        {
            itemName = itemName.Substring(4); // Remove "Item" prefix
        }

        itemName = itemName.Replace('_', ' '); // Replace underscores with spaces

        // Load the ScriptableObject using its name from Resources/Items directory
        ScriptableObject original = Resources.Load<ScriptableObject>("Items/" + itemName);

        if (original is ItemClass itemClassInstance)
        {
            // Instantiate a clone of the ScriptableObject
            ItemClass clonedItem = Instantiate(itemClassInstance);
            return clonedItem.GetItem();
        }
        else
        {
            AddConsoleOutputText($"Item '{itemName}' not found or is not of correct type.");
            return null;
        }
    }
    
    #endregion
    
    #region Spawning
    
    void SpawnItem(string commandArgs)
    {
        // Splitting itemName and modifications
        string itemName = commandArgs.Split('{')[0].Trim();
        string modifications = "";

        if (commandArgs.Contains('{'))
        {
            if (commandArgs.Contains('}'))
            {
                string[] splitArgs = commandArgs.Split('{', '}');
                if (splitArgs.Length > 1)
                {
                    modifications = splitArgs[1].Trim();
                }
                else
                {
                    AddConsoleOutputText($"Parsing error: Expected content inside {{ }} but got none.");
                }
            }
            else
            {
                AddConsoleOutputText($"Parsing error: Found '{{' but missing '}}'.");
            }
        }


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
        
        // getting index
        int? index = null;
        if (propertyName.EndsWith("]"))
        {
            int startIndex = propertyName.IndexOf("[");
            int endIndex = propertyName.IndexOf("]");
            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                string indexString = propertyName.Substring(startIndex + 1, endIndex - startIndex - 1);
                if (int.TryParse(indexString, out int parsedIndex))
                {
                    index = parsedIndex;
                    propertyName = propertyName.Substring(0, startIndex);
                }
            }
        }

        PropertyInfo propertyInfo = component.GetType().GetProperty(propertyName);
        FieldInfo fieldInfo = component.GetType().GetField(propertyName);
        
        // array stuff
        if (index.HasValue)
        {
            if (propertyInfo != null)
            {
                object propertyObj = propertyInfo.GetValue(component);
                ModifyCollectionAtIndex(component, propertyObj, index.Value, propertyValue, propertyInfo);
                // If the object is an array, then assign it back to the property
                if (propertyObj.GetType().IsArray)
                {
                    propertyInfo.SetValue(component, propertyObj);
                }
            }
            else if (fieldInfo != null)
            {
                object fieldObj = fieldInfo.GetValue(component);
                ModifyCollectionAtIndex(component, fieldObj, index.Value, propertyValue, null, fieldInfo);
                // If the object is an array, then assign it back to the field
                if (fieldObj.GetType().IsArray)
                {
                    fieldInfo.SetValue(component, fieldObj);
                }
            }
            return;
        }

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
        
        Debug.Log($"Property: {propertyName}, Index: {index}, Component:{componentName}");

    }
    
    private void SetValueUsingReflection(object component, Type type, Action<object, object> setValue, string stringValue)
    {
        if (type == typeof(Vector3))
        {
            if (ParseVector3(stringValue, out Vector3 vecResult))
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
        else if (type == typeof(string))
        {
            setValue(component, stringValue);
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
    
    private void ModifyCollectionAtIndex(object component, object collection, int index, string valueString, PropertyInfo propertyInfo = null, FieldInfo fieldInfo = null)
    {
        Type collectionType = collection.GetType();

        // Handling Lists
        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type itemType = collectionType.GetGenericArguments()[0];
            IList list = (IList)collection;

            object value = ConvertValueToType(itemType, valueString);
            if (index >= 0 && index < list.Count)
            {
                list[index] = value;
            }
            else
            {
                list.Add(value);
            }
        }

        // Handling Arrays
        else if (collectionType.IsArray)
        {
            Type itemType = collectionType.GetElementType();
            Array array = (Array)collection;
            object value = ConvertValueToType(itemType, valueString);
    
            // Check if the array needs to be resized
            if (index >= array.Length)
            {
                Array newArray = Array.CreateInstance(itemType, index + 1);
                Array.Copy(array, newArray, array.Length);

                // Filling in-between values with defaults
                for (int i = array.Length; i < index; i++)
                {
                    newArray.SetValue(itemType.IsValueType ? Activator.CreateInstance(itemType) : null, i);
                }

                newArray.SetValue(value, index);
                array = newArray;
            }
            else
            {
                array.SetValue(value, index);
            }

            // Assign the modified array back to the component's field/property
            if (propertyInfo != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    propertyInfo.SetValue(value, array.GetValue(i));
                }
            }
            else if (fieldInfo != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    fieldInfo.SetValue(value, array.GetValue(i));
                }
            }

            // For debugging
            for (int i = 0; i < array.Length; i++)
            {
                AddConsoleOutputText($"Array[{i}] = {array.GetValue(i)}");
            }
        }

        else
        {
            AddConsoleOutputText($"Property is not a list or array.");
        }
    }
    
    #endregion
    
    #region Helper Funcs
    
    private object ConvertValueToType(Type targetType, string valueString)
    {
        if (targetType == typeof(string))
        {
            return valueString;
        }
        else if (targetType == typeof(int))
        {
            return int.Parse(valueString);
        }
        else if (targetType == typeof(bool))
        {
            return bool.Parse(valueString);
        }
        else if (targetType == typeof(float))
        {
            return float.Parse(valueString);
        }
        else if (targetType == typeof(ItemClass))
        {
            return GetItemFromScriptableObject(valueString);
        }
        else if (targetType == typeof(Vector3))
        {
            if (ParseVector3(valueString, out Vector3 vecResult))
            {
                return vecResult;
            }
            throw new InvalidOperationException($"Failed to parse string as Vector3: {valueString}");
        }
        // Add more type conversions as needed.
        throw new InvalidOperationException($"Cannot convert string to type {targetType.Name}");
    }
    
    bool ParseVector3(string input, out Vector3 result)
    {
        result = new Vector3();
        if (input.StartsWith("Vector3(") && input.EndsWith(")"))
        {
            string[] parts = input[8..^1].Split(',');
            if (parts.Length == 3 && 
                float.TryParse(parts[0], out float x) && 
                float.TryParse(parts[1], out float y) && 
                float.TryParse(parts[2], out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }
        }
        return false;
    }

    #endregion
}
