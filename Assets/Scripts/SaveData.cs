using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    //Aqui se crean las variables que se quieran guardar
    public float[] playerPosition; //Se guarda en un arreglo de floats[] == Vector3

    public SaveData(Controller playerController)
    {
        playerPosition = new float[3];

        playerPosition[0] = playerController.transform.position.x;
        playerPosition[1] = playerController.transform.position.y;
        playerPosition[2] = playerController.transform.position.z;
    }
    
}
