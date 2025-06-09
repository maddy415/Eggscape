using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public Transform feetPos;
    public LayerMask groundLayer;
    public float jumpForce = 10;
    public float groundDistance = 0.25f;
    public float moveSpeed;
    public float impForce = 4f;
    public GameObject impulsePos;
    

    private void Start()
    { 
        sprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    private bool isGrounded = false;
    private bool isJumping = false;
    private float jumpTimer = 0;
    public float jumpTime = 0.5f;

    private void Update()
    {
        Jump();
        Move();
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
        Player player = GetComponent<Player>();
       
        
        player.moveSpeed = 0f;
        player.jumpForce = 0f;
        Vector2 direcao = Vector2.left;
        
        rb.AddForce(Vector2.left * impForce, ForceMode2D.Impulse);
        
        rb.linearVelocity = new Vector2(0f, 25f); //tentar aplicar no x e y de um msm obj
        //rb.AddForce(new Vector3(0f, impForce, 3f));
        
        
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            GameManager.Instance.playerAlive = false;
            Debug.Log("morreu burro");
            GameManager.Instance.StopScene();
            Death();
        }
    }
}
