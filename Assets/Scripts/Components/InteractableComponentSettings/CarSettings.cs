using UnityEngine;

public class CarSettings : MonoBehaviour, Settings
{
    public float currentBoost;
    public float accelSpeed;
    public float maxBoost;

    public float CurrentBoost
    {
        get => currentBoost;
        set => currentBoost = value;
    }
    public float AccelerationSpeed => accelSpeed;
    public float MaxBoost => maxBoost;
}
