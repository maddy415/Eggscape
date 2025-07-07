using System;
using UnityEngine;

public class TutorialEgg : MonoBehaviour
{
    
    public float walkTime;
    private float walkTimer;
    public bool isWalkingCutscene = true;
    private SpriteRenderer sprite;
    public GameObject explosion;


    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();

    }

    private void Update()
    {
        walkTimer += Time.deltaTime;
        
        if (isWalkingCutscene)
        {
            transform.position += Vector3.right * Time.deltaTime * 5f;
        }
        
        if (walkTimer > walkTime)
        {
            isWalkingCutscene = false;   
            walkTimer = 0;
            sprite.flipX = true;
        }
        
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            GameObject explosionA = Instantiate(explosion, other.transform.position, other.transform.rotation);
            Destroy(explosionA, 2f);
            Destroy(gameObject);
            AudioManager.audioInstance.ExplodeSFX();
            
        }
    }
}
