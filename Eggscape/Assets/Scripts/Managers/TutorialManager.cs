using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    // ========== REFERÊNCIAS ==========
    public Player player;
    public TutorialEgg nerdEgg;
    public ObstacleGen obsGen;
    public GameObject objectGen;
    
    // ========== UI ==========
    public GameObject textCanvas;
    public TextMeshProUGUI dialogueText;
    
    // ========== CONFIGURAÇÕES DA CUTSCENE ==========
    public float walkTime;
    public float spawnTime;
    
    // ========== CONTROLE DE ESTADO ==========
    private float walkTimer;
    private bool onCutscene = true;
    private bool isWalkingCutscene = true;
    private int currentIndex = 0;
    private bool hasSpawned = false;
    
    // MUDANÇA: Nova flag para controlar se o primeiro diálogo já foi mostrado
    private bool firstDialogueShown = false;
    
    // ========== DIÁLOGOS ==========
    private string[] dialogues = {
        "Chicken, precisamos correr, as outras galinhas estão nos esperando!",
        "Primeiro, vou te ensinar a pular troncos. Tem um vindo aí, aperte 'Espaço' para pular!",
    };

    // ========== INICIALIZAÇÃO ==========
    private void Start()
    {
        // Busca as referências necessárias
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        nerdEgg = GameObject.FindWithTag("TutorialEgg").GetComponent<TutorialEgg>();
        obsGen = objectGen.GetComponent<ObstacleGen>();
        
        // Inicia com o canvas de texto desativado
        textCanvas.SetActive(false);
    }

    // ========== LOOP PRINCIPAL ==========
    private void Update()
    {
        HandlePlayerMovement();
        HandleWalkingCutscene();
        HandleDialogueDisplay();
        HandleInput();
    }

    // ========== CONTROLE DE MOVIMENTO ==========
    private void HandlePlayerMovement()
    {
        // Controla se o jogador pode se mover baseado na cutscene
        if (onCutscene)
        {
            player.CanMove = false;
        }
        else
        {
            player.CanMove = true;
        }
    }

    private void HandleWalkingCutscene()
    {
        // Incrementa o timer enquanto a cutscene de caminhada está ativa
        walkTimer += Time.deltaTime;
        
        // Move o player para a direita durante a cutscene
        if (isWalkingCutscene)
        {
            player.transform.position += Vector3.right * Time.deltaTime * 5f;
        }
        
        // Para a caminhada quando o tempo acabar
        if (walkTimer > walkTime)
        {
            isWalkingCutscene = false;   
            walkTimer = 0;
        }
    }

    // ========== CONTROLE DE DIÁLOGO ==========
    private void HandleDialogueDisplay()
    {
        // MUDANÇA: Agora ativa o canvas e mostra o primeiro diálogo automaticamente
        // quando ambos os personagens pararem de andar
        if (nerdEgg.isWalkingCutscene == false && !firstDialogueShown)
        {
            textCanvas.SetActive(true);
            // Mostra o primeiro diálogo imediatamente
            ChangeText();
            firstDialogueShown = true;
        }
    }

    private void HandleInput()
    {
        // Detecta clique do mouse para avançar os diálogos
        if (Input.GetMouseButtonDown(0))
        {
            // MUDANÇA: Só avança o índice se já tiver mostrado o primeiro diálogo
            if (firstDialogueShown)
            {
                currentIndex++;
                ChangeText();
                
                // Spawna obstáculo após o segundo diálogo (índice 1)
                if (currentIndex == 1 && !hasSpawned)
                {
                    StartCoroutine(SpawnDelay());
                }
            }
        }
    }

    private void ChangeText()
    {
        // Atualiza o texto do diálogo se ainda houver diálogos para mostrar
        if (currentIndex < dialogues.Length)
        {
            dialogueText.text = dialogues[currentIndex];
        }
    }

    // ========== COROUTINES ==========
    private IEnumerator SpawnDelay()
    {
        // Libera o jogador para se mover
        onCutscene = false;
        
        // Aguarda x segundos antes de spawnar o obstáculo
        yield return new WaitForSeconds(spawnTime);
        
        // Spawna o primeiro obstáculo
        obsGen.SpawnObstacle();
        hasSpawned = true;
    }
}