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
         GameObject explosionClone = Instantiate(explosion, other.transform.position, other.transform.rotation);
         Destroy(explosionClone, 0.9f);
         
         GameManager.Instance.objsOnScene.Remove(other.gameObject);
         
         GetComponentInParent<Player>().Knockback();
         AudioManager.audioInstance.LogSFX();
      }
      
   }
}
