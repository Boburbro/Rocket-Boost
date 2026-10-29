using UnityEngine;
using UnityEngine.InputSystem;
 
public class QuitApplication : MonoBehaviour
{
  void Update()
  {
    if(Keyboard.current.escapeKey.isPressed)
    {
      Debug.Log("We pushed escape, aren't we clever");
      Application.Quit();
    }
  }
}