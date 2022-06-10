using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestingInputSystem : MonoBehaviour
{
    private InputAction inputAction;

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            //Debug.Log("Jump!" + context.phase);
            //controller.NewJump();
        }
        //Debug.Log("Jump!" + context.phase);
    }
}
