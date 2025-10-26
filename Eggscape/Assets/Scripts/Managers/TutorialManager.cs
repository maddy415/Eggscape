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
    public ObstacleGen obsGen;
    public GameObject objectGen;
    public float walkTime;
    private float walkTimer;
    private bool onCutscene = true;
    bool isWalkingCutscene = true;
    private bool isOnCoroutine = false;
    public GameObject textCanvas;
    public TextMeshProUGUI dialogueText;
    private int currentIndex = 0;



    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        nerdEgg = GameObject.FindWithTag("TutorialEgg").GetComponent<TutorialEgg>();
        obsGen = objectGen.GetComponent<ObstacleGen>();
        textCanvas.SetActive(false);

    }

    private void Update()
    {
        if (player != null)
        {
            player.SetMovementEnabled(!onCutscene);
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
            textCanvas.SetActive(true);
        }

        if (Input.GetMouseButtonDown(0))
        {
            ChangeText();
            currentIndex++;
            
            if (currentIndex == 1 && hasSpawned == false)
            {
               StartCoroutine(SpawnDelay());
            }
        }
        
    }

    private string[] dialogues = {
        "Chicken, sou o TutoriOvo e vou te ensinar como sobreviver a este mundo cruel.",
        "Primeiro, vou te ensinar a pular troncos. Tem um vindo aí, aperte 'Espaço' para pular!",
        "Mandou ver. Você tbm pode quebrar os troncos atacando-os"
    };

    void ChangeText()
    {
        if (currentIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentIndex];
        }
        
    }
    
    bool hasSpawned = false;

    IEnumerator SpawnDelay()
    {
        onCutscene = false;
        yield return new WaitForSeconds(3f);
        obsGen.SpawnObstacle();
        hasSpawned = true;
        

    }
    // IEnumerator TextTiming()
    // {
    //     isOnCoroutine = true;
    //     textCanvas.SetActive(true);
    //
    //     while (currentIndex < dialogues.Length)
    //     {
    //         if (currentIndex != 0)
    //         { 
    //             yield return new WaitForSeconds(2f);
    //         }
    //         ChangeText();
    //         
    //         
    //         if (currentIndex == 1 && hasSpawned == false)
    //         {
    //             yield return StartCoroutine(SpawnDelay());
    //         }
    //
    //         currentIndex++;
    //
    //         
    //         
    //     }
    //
     }
    
    
    

