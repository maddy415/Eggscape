using System;
using UnityEngine;

public class AttackCollisionHandler : MonoBehaviour
{
    public GameObject explosion;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1) Se for “Obstacle”, mantém seu comportamento atual
        if (other.CompareTag("Obstacle"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.objsOnScene.Remove(other.gameObject);
            GameManager.Instance.UpdateScore(2);

            GetComponentInParent<Player>().Knockback();
            AudioManager.audioInstance.LogSFX();
            return;
        }

        // 2) Se acertar o BOSS → causar dano + knockback
        var boss = other.GetComponentInParent<BossSimpleController>() 
                   ?? other.GetComponent<BossSimpleController>();
        if (boss != null)
        {
            var player = GetComponentInParent<Player>();
            if (player != null && player.IsAttackActive)
            {
                // tira vida do boss
                boss.TakeDamage(player.attackDamage);

                // feedback opcional
                /*if (explosion) {
                    var fx = Instantiate(explosion, other.bounds.ClosestPoint(transform.position), Quaternion.identity);
                    Destroy(fx, 1.1f);
                }*/

                // dá knockback no player como “recoil” do golpe
                player.Knockback();

                // (opcional) som de hit
                // AudioManager.audioInstance.HitSFX();

                return;
            }
        }
    }
}