using System;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    // manage input
    // manage raycast, movement, positions
    // communicate with world manager
    private DataTypes.ControllerInput controllerInput;

    public DataTypes.ControllerInput ControllerInput
    {
        get => controllerInput;
        set => controllerInput = value;
    }
    private void Start()
    {
        throw new NotImplementedException();
    }
}
