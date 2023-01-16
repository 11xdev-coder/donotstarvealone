using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Animator animator;

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        float horizontal = 0f;
        float vertical = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            vertical = 1f;
            animator.Play("walkbackward");
        }
        if (Input.GetKey(KeyCode.A))
        {
            horizontal = -1f;
            animator.Play("walkleft");
        }
        if (Input.GetKey(KeyCode.S))
        {
            vertical = -1f;
            animator.Play("walkfront");
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1f;
            animator.Play("walkright");
        }
        movement = new Vector2(horizontal, vertical);
    }

    private void FixedUpdate()
    {
        // Move the player based on the input
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}