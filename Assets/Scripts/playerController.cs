using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    public Rigidbody2D playerRB;
    public int speed = 10;
    // Start is called before the first frame update
    void Start()
    {
        
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
            playerRB.velocity = new Vector2(0, playerRB.velocity.y);
        }

        // Check for up and down input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // Move the player up
            playerRB.velocity = new Vector2(playerRB.velocity.x, speed);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // Move the player down
            playerRB.velocity = new Vector2(playerRB.velocity.x, -speed);
        }
        else
        {
            // Stop the player vertically
            playerRB.velocity = new Vector2(playerRB.velocity.x, 0);
        }
    }
}