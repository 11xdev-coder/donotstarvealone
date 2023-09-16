using System;
using UnityEngine;

public class InteractableComponent : MonoBehaviour
{
    [Header("-- Important --")]
    public InteractableActionList actionList;
    private Action<GameObject, GameObject, Settings> _currentInteractAction;
    public InteractableType type;

    public Settings settings;

    public enum InteractableType
    {
        Car
    }

    private void Start()
    {
        switch (type)
        {
            case InteractableType.Car:
                settings = GetComponent<CarSettings>();
                _currentInteractAction = actionList.EnterCar;
                break;
            
        }
    }

    public void Interact(GameObject doer)
    {
        if (_currentInteractAction != null)
        {
            _currentInteractAction(doer, gameObject, settings);
        }
        else
        {
            Debug.LogError("InteractAction is not assigned!");
        }
    }

}
