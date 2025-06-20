using System;
using UnityEngine;

public class AttackCollisionHandler : MonoBehaviour
{
   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.CompareTag("Obstacle"))
      {
         Destroy(other.gameObject);
         Debug.Log("tocou");
         GameManager.Instance.objsOnScene.Remove(other.gameObject);
         
         GetComponentInParent<Player>().Knockback();
      }
      
   }
}
