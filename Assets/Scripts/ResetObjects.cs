using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetObjects : MonoBehaviour
{
    [SerializeField] private GameObject[] scene_objects;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            var delete_objects = FindObjectsOfType<GameObject>();

            foreach(var obj in delete_objects)
            {
                if(!obj.CompareTag("Ground") && !obj.CompareTag("MainCamera"))
                {
                    Destroy(obj);
                }
            }

            foreach(var obj in scene_objects)
            {
                Instantiate(obj);
            }

        }
    }



}
