using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Animator animator;
    public Camera camera;

    private Rigidbody2D rb;
    public Vector2 movement;
    public Vector2 mousePosition;
    public Vector2 moveDirection;
    
    public float horizontal;
    public float vertical;
    public bool diagonal = false;
    public bool playsidewaysAnim;

    private void Awake()
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

        if (Input.GetMouseButton(0))
        {
            mousePosition = camera.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition); // convert mouse screen position to world position
            moveDirection = mousePosition - rb.position; // get the direction from player to mouse position
            horizontal = Mathf.Clamp(moveDirection.x, -1f, 1f); // limit the horizontal value to -1 and 1
            vertical = Mathf.Clamp(moveDirection.y, -1f, 1f); // limit the vertical value to -1 and 1
            
        }
        
        // idle check
        if (!Input.GetKey(KeyCode.A) | !Input.GetKey(KeyCode.W) | !Input.GetKey(KeyCode.S) | !Input.GetKey(KeyCode.D))
        {
            PlayAnimation();
        }
        // is s or d pressed
        if (Input.GetKeyUp(KeyCode.S) | Input.GetKeyUp(KeyCode.D))
        {
            diagonal = false;
        }
        movement = new Vector2(horizontal, vertical);
    }

    public void MoveToMousePosition()
    {
        
    }

    private void FixedUpdate()
    { 
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}