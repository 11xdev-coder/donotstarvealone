using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [Header("animations")] 
    public Animator animator;
    public AnimatorController walkController;
    
    public Rigidbody2D playerRB;
    public int speed = 10;
    
    
    // Start is called before the first frame update
    void Start()
    {
        animator.runtimeAnimatorController = walkController;
        animator.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Check for left and right input
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // Move the player to the left
            playerRB.velocity = new Vector2(-speed, playerRB.velocity.y);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            // Move the player to the right
            playerRB.velocity = new Vector2(speed, playerRB.velocity.y);
        }
        else
        {
            // Stop the player horizontally
            animator.enabled = false;
            playerRB.velocity = new Vector2(0, playerRB.velocity.y);
        }

        // Check for up and down input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // Move the player up
            playerRB.velocity = new Vector2(playerRB.velocity.x, speed);
            animator.enabled = true;
            animator.Play("walkbackward");
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // Move the player down
            playerRB.velocity = new Vector2(playerRB.velocity.x, -speed);
            animator.enabled = true;
            animator.Play("walkfront");
        }
        else
        {
            // Stop the player vertically
            animator.enabled = false;
            playerRB.velocity = new Vector2(playerRB.velocity.x, 0);
        }
    }
}