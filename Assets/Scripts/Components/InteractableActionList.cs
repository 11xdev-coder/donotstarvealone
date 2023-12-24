using System.Linq;
using UnityEngine;

namespace Components
{
    public class InteractableActionList : MonoBehaviour
    {
        public void EnterCar(GameObject doer, GameObject caller, Settings settings)
        {
            PlayerController pc = doer.GetComponent<PlayerController>();
            pc.ToggleVisibility(true);
            pc.isInCar = true; // we are in car
            pc.car = caller; // set car to caller (Car object)
            pc.currentInteractionSettings = settings;
        }

        public void ExitCar(GameObject doer)
        {
            PlayerController pc = doer.GetComponent<PlayerController>();
            pc.ToggleVisibility(false);
            pc.isInCar = false;
            pc.car = null;
            pc.currentInteractionSettings = null;
        }   
    }
}
