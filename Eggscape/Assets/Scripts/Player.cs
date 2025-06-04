using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public Rigidbody2D rb;
    public Transform feetPos;
    public LayerMask groundLayer;
    public float jumpForce = 10;
    public float groundDistance = 0.25f;
    public float moveSpeed;
 
    
    private bool isGrounded = false;
    private bool isJumping = false;
    public  bool isAlive = true;
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
            transform.eulerAngles = new Vector3(0, 0, 0);
        }
        else if (moveInput < 0)
        {
            transform.eulerAngles = new Vector3(0, 180, 0);
        }
    }

    private void Death()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            //isAlive = false;
            Debug.Log("morreu burro");
            GameManager.Instance.StopScene();
        }
    }
}
