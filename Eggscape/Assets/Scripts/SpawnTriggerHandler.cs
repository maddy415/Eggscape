using System;
using UnityEngine;

public class SpawnTriggerHandler : MonoBehaviour
{
   public bool TriggeredSpawn = false;
   
   
   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.CompareTag("SpawnNextTrigger"))
      {
         Debug.Log(other.name);
         TriggeredSpawn = true;
      }
   }
   
   
}
