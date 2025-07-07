using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{

    public Player player;
    public TutorialEgg nerdEgg;
    public float walkTime;
    private float walkTimer;
    private bool onCutscene = true;
    bool isWalkingCutscene = true;
    private bool isOnCoroutine = false;
    public GameObject textCanvas;
    public TextMeshProUGUI dialogueText;


    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        nerdEgg = GameObject.FindWithTag("TutorialEgg").GetComponent<TutorialEgg>();
        textCanvas.SetActive(false);

    }

    private void Update()
    {
        if (onCutscene)
        {
            player.canMove = false;
        }
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

        if (nerdEgg.isWalkingCutscene==false)
        {
            if (isOnCoroutine==false)
            {
                StartCoroutine(TextTiming());
            }
            
        }
        
    }

    private string[] dialogues = {
        "Primeiro, vou te ensinar a pular troncos. Tem um vindo aí!",
        "Mandou ver. Você tbm pode quebrar os troncos atacando-os"
    };

    private int currentIndex = 0;
    void ChangeText()
    {
        if (currentIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentIndex];
        }
        else 
        {
            Debug.Log("cabo os dialogo meu truta");
            StopAllCoroutines();
        }
    }


    IEnumerator TextTiming()
    {
        isOnCoroutine = true;
        textCanvas.SetActive(true);

        while (currentIndex < dialogues.Length)
        {
            yield return new WaitForSeconds(2f);
            ChangeText();
            currentIndex++;
        }
        


    }
}
