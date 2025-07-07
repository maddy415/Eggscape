using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{

    public Player player;
    public float walkTime;
    private float walkTimer;
    private bool onCutscene = false;
    bool isWalkingCutscene = true;

    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        
    }

    private void Update()
    {
        walkTimer += Time.deltaTime;
        
        if (isWalkingCutscene)
        {
            player.transform.position += Vector3.right * Time.deltaTime * 5f;
        }
        
        if (walkTimer > walkTime)
        {
            isWalkingCutscene = false;   
            walkTimer = 0;
        }
        
    }


    /*IEnumerator WalkThenStop()
    {
        for (walkTime = 0; walkTime < 2; walkTime++)
        {
            walkTime += Time.deltaTime;
            player.transform.Translate(Vector3.right * player.transform.position.x);
        }
    }*/
}
