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
    public float jumpForce = 10;
    public float groundDistance = 0.25f;
    public float moveSpeed;
    public float impForce = 4f;
    public GameObject impulsePos;
    private bool playerDead = false;
    

    private void Start()
    { 
        sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    private bool isGrounded = false;
    private bool isJumping = false;
    private float jumpTimer = 0;
    private bool canMove = true;
    
    public float jumpTime = 0.5f;

    private void Update()
    {
        if (playerDead)
        {
            
        }
        if (canMove)
        {
            Move();
            Jump();
        }

        if (isGrounded && GameManager.Instance.victoryAchieved)
        {
            canMove = false;
            transform.position += Vector3.right * Time.deltaTime * 10f;
        }
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

  
}
