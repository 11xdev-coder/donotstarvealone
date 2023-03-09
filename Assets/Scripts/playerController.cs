using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Animator animator;

    private Rigidbody2D _rb;
    public Vector2 movement;
    public Vector2 targetPos;
    public Vector2 direction;
    private bool _moveToMouse;
    
    public float horizontal;
    public float vertical;
    public bool diagonal;
    public bool playsidewaysAnim;

    public InventoryManager inventory;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private bool AnimationIsOver(Animator animator, int layer)
    {
        if (animator.GetCurrentAnimatorStateInfo(layer).normalizedTime > 1)
        {
            return true;
        }
        return false;
    }
    
    private void PlayAnimation()
    {
        if (AnimationIsOver(animator, 0))
        {
            if (horizontal == 0 & vertical < 0)
                if(!diagonal) animator.Play("walkfront");
            
            if (horizontal == 0 & vertical > 0)
                if(!diagonal) animator.Play("walkbackward");
        
            
        
            if ((vertical == 0 & horizontal > 0) || (vertical != 0 & horizontal > 0)) animator.Play("walkright");
            if ((vertical == 0 & horizontal < 0) || (vertical != 0 & horizontal < 0)) animator.PlayInFixedTime("walkleft");
        }
    }

    private void Idle()
    {
        if (playsidewaysAnim) animator.PlayInFixedTime("idlesideways");
        else animator.PlayInFixedTime("idle");
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;
        // if w pressed
        if (Input.GetKey(KeyCode.W))
        {
            playsidewaysAnim = false;
            vertical = 1;
            PlayAnimation();
        }
        // if a pressed
        if (Input.GetKey(KeyCode.A))
        {
            playsidewaysAnim = true;
            diagonal = true;
            horizontal = -1;
            PlayAnimation();
        }
        // if d pressed
        if (Input.GetKey(KeyCode.D))
        {
            playsidewaysAnim = true;
            diagonal = true;
            horizontal = 1;
            PlayAnimation();
        }
        // if s pressed
        if (Input.GetKey(KeyCode.S))
        {
            playsidewaysAnim = false;
            vertical = -1;
            PlayAnimation();
        }
        
        
        // if left clicked pressed
        if (Input.GetMouseButton(0))
        {
            _moveToMouse = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            _moveToMouse = false;
        }
        else
        {
            if (_moveToMouse)
            {
                MoveToMouse();
            }
            // idle check
            if (horizontal <= 0 & vertical <= 0)
            {
                Idle();
            }
            // is s or d pressed
            if (Input.GetKeyUp(KeyCode.A) | Input.GetKeyUp(KeyCode.D))
            {
                diagonal = false;
            }
        }

        
        
        movement = new Vector2(horizontal, vertical);
        _rb.MovePosition(_rb.position + movement * moveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.E))
        {
            // use selected item
            if(inventory.equippedTool != null)
                inventory.equippedTool.item.Use(this);
        }
    }

    private void MoveToMouse()
    {
        targetPos = Input.mousePosition / 4;
        direction = (targetPos - _rb.position).normalized;
        horizontal = Math.Clamp(direction.x, -1, 1);
        vertical = Math.Clamp(direction.y, -1, 1);
        PlayAnimation();
    }
}