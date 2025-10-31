using System;
using UnityEngine;

public class AttackCollisionHandler : MonoBehaviour
{
   public GameObject explosion;
   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.CompareTag("Obstacle"))
      {

         Destroy(other.gameObject);
         GameManager.Instance.objsOnScene.Remove(other.gameObject);
         GameManager.Instance.UpdateScore();

         
         GetComponentInParent<Player>().Knockback();
         AudioManager.audioInstance.LogSFX();
      }
      
   }
}
