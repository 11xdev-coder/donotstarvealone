using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    [Header("Sliders")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    
    [Header("Text")]
    public TMP_Text redValue;
    public TMP_Text greenValue; 
    public TMP_Text blueValue;
    
    [Header("Display")]
    public Image displayImage;
    public TMP_InputField hexInputField;
    
    private bool m_IsUpdatingHex = false; // A flag to avoid recursive updates

    public void Start()
    {
        redSlider.onValueChanged.AddListener(UpdateColorFromSliders);
        greenSlider.onValueChanged.AddListener(UpdateColorFromSliders);
        blueSlider.onValueChanged.AddListener(UpdateColorFromSliders);
        
        hexInputField.onEndEdit.AddListener(UpdateColorFromHex);
        
        UpdateColorFromSliders(1f);
        
        gameObject.SetActive(false);
    }

    private void UpdateColorFromSliders(float _)
    {
        if (m_IsUpdatingHex) return;
        
        float r = redSlider.value / 255f;
        float g = greenSlider.value / 255f;
        float b = blueSlider.value / 255f;

        redValue.text = (r * 255).ToString(CultureInfo.CurrentCulture);
        greenValue.text = (g * 255).ToString(CultureInfo.CurrentCulture);
        blueValue.text = (b * 255).ToString(CultureInfo.CurrentCulture);

        Color color = new Color(r, g, b);
        displayImage.color = color;
        
        UpdateHexInput(color);
    }

    private void UpdateHexInput(Color color)
    {
        string hexCode = ColorUtility.ToHtmlStringRGB(color);
        hexInputField.text = '#' + hexCode;
    }
    
    private void UpdateColorFromHex(string hexCode)
    {
        if (ColorUtility.TryParseHtmlString(hexCode, out Color color))
        {
            m_IsUpdatingHex = true; // Set the flag to avoid recursive updates

            float r = redSlider.value = color.r * 255;
            float g = greenSlider.value = color.g * 255;
            float b = blueSlider.value = color.b * 255;
            
            redValue.text = r.ToString(CultureInfo.CurrentCulture);
            greenValue.text = g.ToString(CultureInfo.CurrentCulture);
            blueValue.text = b.ToString(CultureInfo.CurrentCulture);

            displayImage.color = color;

            m_IsUpdatingHex = false; // Reset the flag
        }
    }

    public void ShowMenu() => gameObject.SetActive(!gameObject.activeSelf);
}
