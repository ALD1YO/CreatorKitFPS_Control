using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public GameObject player;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            //player.GetComponent<Controller>().SavePlayer();
            //Debug.Log("P Pressed: Save");
                
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            //player.GetComponent<Controller>().LoadPlayer();
            //Debug.Log("L Pressed: Load");
            //player.transform.position = new Vector3(0, 0, 0);
        }
    }
}
