using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Animator animator;

    private Rigidbody2D rb;
    private Vector2 movement;
    
    public float horizontal;
    public float vertical;
    public bool diagonal = false;
    public bool playsidewaysAnim;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void PlayAnimation()
    {
        if (horizontal == 0 & vertical > 0)
            if(!diagonal) animator.Play("walkbackward");
        
        if (horizontal == 0 & vertical < 0)
            if(!diagonal) animator.Play("walkfront");
        
        if (vertical == 0 & horizontal > 0 || vertical != 0 & horizontal > 0) animator.Play("walkright");
        if (vertical == 0 & horizontal < 0 || vertical != 0 & horizontal < 0) animator.Play("walkleft");

        if (vertical == 0 & horizontal == 0)
        {
            if (playsidewaysAnim) animator.Play("idlesideways");
            else animator.Play("idle");
        }
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;
        
        if (Input.GetKey(KeyCode.W))
        {
            playsidewaysAnim = false;
            vertical = 1f;
            PlayAnimation();
        }
        if (Input.GetKey(KeyCode.A))
        {
            playsidewaysAnim = true;
            diagonal = true;
            horizontal = -1f;
            PlayAnimation();
        }
        if (Input.GetKey(KeyCode.S))
        {
            playsidewaysAnim = false;
            vertical = -1f;
            PlayAnimation();
        }
        if (Input.GetKey(KeyCode.D))
        {
            playsidewaysAnim = true;
            diagonal = true;
            horizontal = 1f;
            PlayAnimation();
        }
        
        // idle check
        if (!Input.GetKey(KeyCode.A) | !Input.GetKey(KeyCode.W) | !Input.GetKey(KeyCode.S) | !Input.GetKey(KeyCode.D))
        {
            PlayAnimation();
        }

        if (Input.GetKeyUp(KeyCode.S) | Input.GetKeyUp(KeyCode.D))
        {
            diagonal = false;
        }
        movement = new Vector2(horizontal, vertical);
    }

    private void FixedUpdate()
    {
        // Move the player based on the input
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}