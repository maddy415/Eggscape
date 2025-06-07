using System;
using UnityEngine;

public class DestroyObs : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Obstacle")
        {
            Destroy(other.gameObject);
        }
    }
}
