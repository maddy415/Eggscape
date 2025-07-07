using System;
using UnityEngine;

public class TutorialEgg : MonoBehaviour
{
    
    public float walkTime;
    private float walkTimer;
    public bool isWalkingCutscene = true;
    private SpriteRenderer sprite;


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
}
