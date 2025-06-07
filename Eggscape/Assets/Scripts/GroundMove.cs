using System;
using UnityEngine;

public class GroundMove : MonoBehaviour
{
    public float gSpeed = 4;
    private void Update()
    {
        transform.Translate(Vector3.left * gSpeed * Time.deltaTime, Space.World);
    }

    
}
