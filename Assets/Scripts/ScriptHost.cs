using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptHost
{
    public void Log(string text) => Debug.Log(text);
    public void Log(int number) => Debug.Log(number);
}
