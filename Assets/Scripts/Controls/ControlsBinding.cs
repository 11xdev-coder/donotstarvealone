using UnityEngine;

[CreateAssetMenu(menuName = "Controls/ControlBindings")]
public class ControlBindings : ScriptableObject
{
    public KeyCode OpenDebugMenu = KeyCode.H;
    public KeyCode OpenConsole = KeyCode.Tilde;
}
