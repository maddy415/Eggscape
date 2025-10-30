using System;
using UnityEngine;

public class TutorialEgg : MonoBehaviour
{
    [Header("Movimento inicial (cutscene)")]
    public float walkTime = 2f;          // Duração da caminhada antes da "voada"
    private float walkTimer;
    public bool isWalkingCutscene = true;

    [Header("Componentes")]
    private SpriteRenderer sprite;
    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;
    public TutorialManager tutorialManager;


    [Header("Explosão e física da voada")]
    public GameObject explosion;
    public float impactForce = 1f;       // Multiplica a força do impacto
    public float torque = 300f;          // Força de rotação física (já existia)

    [Header("Giro visual adicional")]
    [Tooltip("Velocidade de rotação adicional durante a voada (graus por segundo).")]
    public float spinSpeed = 720f;       // ⚙️ Novo: rotação visual extra
    private bool isFlying = false;       // ⚙️ Novo: flag para saber se está na 'voada'


    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        walkTimer += Time.deltaTime;
        
        // Caminhada da cutscene
        if (isWalkingCutscene)
        {
            transform.position += Vector3.right * Time.deltaTime * 5f;
        }

        // Quando acabar o tempo de caminhada, ele vira e para
        if (walkTimer > walkTime)
        {
            isWalkingCutscene = false;   
            walkTimer = 0;
            sprite.flipX = true;
        }

        // ⚙️ Novo: se estiver "voando", gira visualmente
        if (isFlying)
        {
            // Rotação contínua no eixo Z (sem interferir na física)
            transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            tutorialManager.CloseDialogueBox();
            tutorialManager.ShowGeneralDialogue();
            
            StartCoroutine(tutorialManager.GeneralWalk());
            // Cria explosão
            GameObject explosionA = Instantiate(explosion, other.transform.position, other.transform.rotation);
            Destroy(explosionA, 2f);

            // Impulso físico para trás e pra cima
            rb.linearVelocity = new Vector2(-30f, 25f) * impactForce * Time.deltaTime;

            // Desativa colisão pra não bater de novo
            bodyCollider.enabled = false;

            // Libera rotação física
            rb.freezeRotation = false;

            // Adiciona torque físico (faz ele girar de forma mais "caótica")
            rb.AddTorque(torque);

            // ⚙️ Novo: ativa rotação visual adicional
            isFlying = true;

            // Som de explosão
            AudioManager.audioInstance.ExplodeSFX();
        }
    }
}
