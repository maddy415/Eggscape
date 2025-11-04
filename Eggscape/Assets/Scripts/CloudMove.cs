using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class CloudMove : MonoBehaviour
{
    public float moveSpeed;
    private List<GameObject> clouds = new List<GameObject>();
    private void Update()
    {
        //transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

        foreach (Transform child in gameObject.transform)
        {
            clouds.Add(child.gameObject);
        }
    }
}
