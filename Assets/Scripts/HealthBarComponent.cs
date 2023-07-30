using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarComponent : MonoBehaviour
{
    [Header("-- Assignable - Important --")]
    public GameObject healthBarPrefab;
    public GameObject thisObject;  // The enemy that this health bar belongs to
    public float yOffset;
    public Gradient gradient;
    
    [Header("-- Debug --")]
    public GameObject healthBarInstance; // The instantiated health bar
    public TMP_Text healthBarText;
    public Slider slider;  // The UI Image component of this health bar
    public Image fill;

    private Camera m_Camera;
    private Canvas m_Canvas;
    private AttackableComponent m_ThisObjectHealth;
    
    // important to make this awake and in HealthBarComponent holder set this up in Start, otherwise it will give an error
    private void Awake()
    {
        m_Canvas = FindObjectOfType<Canvas>();
        m_Camera = FindObjectOfType<Camera>();
        
        // Instantiate a new health bar from the prefab
        var position = thisObject.transform.position;
        healthBarInstance = Instantiate(healthBarPrefab, new Vector3(position.x, position.y - yOffset, position.z), Quaternion.identity, m_Canvas.transform);
        
        // get every component we need
        slider = healthBarInstance.GetComponent<Slider>();
        fill = healthBarInstance.GetComponentInChildren<Image>();
        healthBarText = healthBarInstance.GetComponentInChildren<TMP_Text>();

        m_ThisObjectHealth = thisObject.GetComponent<AttackableComponent>();
    }

    // use this in Start to setup the healthbar
    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
        fill.color = gradient.Evaluate(1f);
        healthBarText.text = $"{health}/{slider.maxValue}";
    }
    
    // use this to adjust the fill of the healthbar
    public void SetHealth(int health)
    {
        // Update the health bar to reflect the enemy's current health
        slider.value = health;
        fill.color = gradient.Evaluate(slider.normalizedValue);
        
        healthBarText.text = $"{health}/{slider.maxValue}";

        // The reason why i'm not destroying healthbar here because the object who has this component destroys first on death
        // Better put it in HandleDeath
    }

    private void Update()
    {
        // Position the health bar at the enemy's screen position
        var position = thisObject.transform.position;
        healthBarInstance.transform.position = m_Camera.WorldToScreenPoint(new Vector3(position.x, position.y - yOffset, position.z));
    }
}
