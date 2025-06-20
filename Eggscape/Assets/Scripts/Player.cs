using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    
    public Rigidbody2D rb;
    public BoxCollider2D bc;
    public SpriteRenderer sprite;
    public Transform feetPos;
    public LayerMask groundLayer;
    public BoxCollider2D attackHB;
    
    
    public float jumpForce = 10;
    public float groundDistance = 0.25f;
    public float moveSpeed;
    public float impForce = 4f;
    public float jumpTime = 0.5f;
    private float jumpTimer = 0;
    public float attackAirTime;
    private float attackTimer;
    public float attackCD;
    public float defaultGS;
    public float attackForce;
    private float attackCDtimer;
    
    
    private bool playerDead = false;
    private bool isGrounded = false;
    private bool isJumping = false;
    private bool canMove = true;
    private bool isAttacking = false;
    private bool canAttack = true;
    

    private void Start()
    { 
        sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        rb.gravityScale = defaultGS;
        
    }
    
    

    private void Update()
    {
        if (playerDead)
        {
            
        }

        if (isAttacking && chamouKB==false)
        {
            //transform.position += Vector3.right * Time.deltaTime * attackForce;
            rb.linearVelocity = new Vector2(attackForce, 0);
        }
        if (canMove)
        {
            Move();
            Jump();
        }

        if (isGrounded && GameManager.Instance.victoryAchieved)
        {
            canMove = false;
            transform.position += Vector3.right * Time.deltaTime * 15f;
        }
        Attack();
    }
    private void Jump()
    {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, groundDistance, groundLayer);

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            isJumping = true;
            rb.linearVelocity = Vector2.up * jumpForce;
        }

        if (isJumping && Input.GetButton("Jump"))
        {
            if (jumpTimer < jumpTime)
            {
                rb.linearVelocity = Vector2.up * jumpForce;

                jumpTimer += Time.deltaTime;
            }
            else
            {
                isJumping = false;
                jumpTimer = 0;
                
            }
            
            //Debug.Log($"isGrounded: {isGrounded}, isJumping: {isJumping}, jumpTimer: {jumpTimer}");
        }
    }
    private void Move()
    {
        
        float moveInput = Input.GetAxisRaw("Horizontal"); 
        transform.position += new Vector3(moveInput, 0, 0) * moveSpeed * Time.deltaTime;

        
        if (moveInput > 0)
        {
            sprite.flipX = false;

        }
        else if (moveInput < 0)
        {
            sprite.flipX = true;
        }
        
    }
    private void Death()
    {
        playerDead = true;
        Player player = GetComponent<Player>();
       
        
        player.moveSpeed = 0f;
        player.jumpForce = 0f;


        
        rb.linearVelocity = new Vector2(-10f, 25f) * impForce * Time.deltaTime; 
        bc.enabled = false;
        //bc.isTrigger = true;
        rb.freezeRotation = false;
        rb.AddTorque(40f);
        
        
 
        
    }

    void Attack()
    {
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            attackHB.enabled = true;
            
            attackTimer = 0f;
            isAttacking = true;
            rb.gravityScale = 0;
            //rb.linearVelocity = Vector2.right * 8f;
            
            
            
            /*Se deixar sem nenhum codigo pra mexer a galinha, vira basicamente uma feature nova q ela fica
             descendo lento e pulando alto qnd ataca*/
        }

        if (canAttack==false) //Cooldown do ataque
        {
            attackCDtimer += Time.deltaTime;
            
            if (attackCDtimer >= attackCD)
            {
                canAttack = true; //Libera pra atacar novamente
                attackCDtimer = 0f;
                chamouKB = false;
                
            }
            
        }

        if (isAttacking) //Lógica pra fazer o player cair de volta dps de X segundos no ar
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackAirTime)
            {
                rb.gravityScale = defaultGS;
                Debug.Log("passou o tempo");
                attackTimer = 0f;
                isAttacking = false;
                if (chamouKB == false)
                {
                    rb.linearVelocity = Vector3.zero;
                }
                
                attackHB.enabled = false;
            }
            
        }
        
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            //other.gameObject.GetComponent<BoxCollider2D>().enabled = false;
            GameManager.Instance.playerAlive = false;
            Debug.Log("morreu burro");
            GameManager.Instance.StopScene();
            Death();
        }
        
        
    }
    public bool chamouKB = false;
    public float kbForce;

    public void Knockback()
    {
        //rb.AddForce(Vector2.left * 10f, ForceMode2D.Impulse);
        rb.linearVelocity = new Vector3(-kbForce, rb.linearVelocity.y, 0f);
        Debug.Log("knockback");
        chamouKB = true;
    }
    
}
