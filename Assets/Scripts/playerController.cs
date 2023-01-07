using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [Header("animations")] 
    public Animator animator;
    public RuntimeAnimatorController walkController;
    
    public Rigidbody2D playerRB;
    public int speed = 10;
    
    
    // Start is called before the first frame update
    void Start()
    {
        animator.runtimeAnimatorController = walkController;
        animator.enabled = false;
    }

    void StopAnimator()
    {
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
            animator.enabled = true;
            animator.Play("walkleft");
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            // Move the player to the right
            playerRB.velocity = new Vector2(speed, playerRB.velocity.y);
            animator.enabled = true;
            animator.Play("walkright");
        }
        else
        {
            // Stop the player horizontally
            Invoke("StopAnimator", animator.GetCurrentAnimatorStateInfo(0).length);
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
            Invoke("StopAnimator", animator.GetCurrentAnimatorStateInfo(0).length);
            animator.StopPlayback();
            playerRB.velocity = new Vector2(playerRB.velocity.x, 0);
        }
    }
}