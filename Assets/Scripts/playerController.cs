using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [Header("animations")] 
    public Animator animator;
    public RuntimeAnimatorController walkController;
    public string currentSide;
    
    public Rigidbody2D playerRB;
    public int speed = 10;
    
    
    // Start is called before the first frame update
    void Start()
    {
        animator.runtimeAnimatorController = walkController;
        animator.enabled = true;
    }

    void Playidle()
    {
        if (currentSide == "north" || currentSide == "south")
            animator.Play("idle");
        else if (currentSide == "east" || currentSide == "west")
            animator.Play("idlesideways");
    }

    // Update is called once per frame
    void Update()
    {
        // Check for left and right input
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // Move the player to the left
            playerRB.velocity = new Vector2(-speed, playerRB.velocity.y);
            animator.Play("walk");
            currentSide = "east";
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            // Move the player to the right
            playerRB.velocity = new Vector2(speed, playerRB.velocity.y);
            animator.Play("walk");
            currentSide = "west";
        }
        else
        {
            // Stop the player horizontally
            playerRB.velocity = new Vector2(0, playerRB.velocity.y);
        }

        // Check for up and down input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // Move the player up
            playerRB.velocity = new Vector2(playerRB.velocity.x, speed);
            animator.Play("walk");
            currentSide = "north";
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // Move the player down
            playerRB.velocity = new Vector2(playerRB.velocity.x, -speed);
            animator.Play("walk");
            currentSide = "south";
        }
        else
        {
            // Stop the player vertically
            playerRB.velocity = new Vector2(playerRB.velocity.x, 0);
        }
    }
}