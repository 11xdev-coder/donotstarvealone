using System;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Animator animator;
    
    [Header("Movement")]
    private Rigidbody2D _rb;
    public Vector2 movement;
    public Vector3 mousePos;

    public Vector3 targetPos;
    public Vector3 direction;
    private bool _moveToMouse;
    public bool canMoveToMouse;
    private bool _isMovingToTarget;
    private bool _isAttacking;
    private float _dotProductForward;
    private float _dotProductRight;
    
    [Header("Animation")]
    public float horizontal;
    public float vertical;
    public bool animLocked;
    
    [Header("Attacking thingies")]
    public GameObject attackTarget;

    public InventoryManager inventory;

    private void Awake()
    {
        canMoveToMouse = true;
        _rb = GetComponent<Rigidbody2D>();
    }

    private void UpdateAnimations()
    {
        if (!animLocked)
        {
            if (movement != Vector2.zero)
            {
                animator.Play("walk");
            }
            else if (_isAttacking)
            {
                animator.Play("attack");
            }
            else
            {
                animator.Play("idle");
            }
        }
    }

    public void OnAttackEnd()
    {
        _isAttacking = false;
    }

    private void MoveTowardsTarget(GameObject target)
    {
        if (_isMovingToTarget)
        {
            // calculating direction
            canMoveToMouse = false;
            targetPos = target.transform.position;
            direction = (targetPos - (Vector3)_rb.position).normalized;
            // moving
            horizontal = direction.x;
            vertical = direction.y;
            
            UpdateAnimations();
        }
        
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Collider2D selfCollider = GetComponent<Collider2D>();
        
        // calculating distance between target's center and self's collider's closest bound
        if (targetCollider != null && selfCollider != null)
        {
            float distance = Vector2.Distance(targetCollider.bounds.ClosestPoint(selfCollider.bounds.center), selfCollider.bounds.center);
    
            if (distance < 0.25f)
            {
                ResetAttackTargetAndMoving();
                
                _isAttacking = true;
                Attack(target);
            }
        }
    }
    
    public void SetAttackTarget(GameObject target)
    {
        attackTarget = target;
        canMoveToMouse = false;
    }

    private void ResetAttackTargetAndMoving()
    {
        // reset attack target and set that we can move to mouse
        canMoveToMouse = true;
        attackTarget = null;
        _isMovingToTarget = false;
        _isAttacking = false;
    }

    private void Attack(GameObject target)
    {
        Transform rbTransform = _rb.transform;
        Vector3 toObjectVector = (target.transform.position - rbTransform.position).normalized;
    
        // Since it's a 2D top-down view game, your forward vector will be along the Y axis
        // Right vector will be along the X axis
        Vector3 playerForward = rbTransform.up;
        Vector3 playerRight = rbTransform.right;

        _dotProductForward = Vector3.Dot(toObjectVector, playerForward);
        _dotProductRight = Vector3.Dot(toObjectVector, playerRight);
        
        animator.SetFloat("Attack X", Mathf.Sign(_dotProductRight));
        animator.SetFloat("Attack Y", Mathf.Sign(_dotProductForward));

        try
        {
            // item in hand
            target.GetComponent<HealthComponent>().TakeDamage(inventory.equippedTool.item.damage);
        }
        catch
        {
            // nothing in hand
            target.GetComponent<HealthComponent>().TakeDamage(5);
        }
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            vertical = 1;
            ResetAttackTargetAndMoving();
        }
        if (Input.GetKey(KeyCode.S))
        {
            vertical = -1;
            ResetAttackTargetAndMoving();
        }
        if (Input.GetKey(KeyCode.A))
        {
            horizontal = -1;
            ResetAttackTargetAndMoving();
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1;
            ResetAttackTargetAndMoving();
        }
        

        UpdateAnimations();
        
        // if we want to move to target
        if (_isMovingToTarget) MoveTowardsTarget(attackTarget); // move

        // if left clicked pressed and we have an attacktarget
        if (Input.GetMouseButtonDown(0) && attackTarget != null) _isMovingToTarget = true;
        else if (Input.GetMouseButton(0))
        {
            _moveToMouse = true;
            if(canMoveToMouse && !_isMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            _moveToMouse = false;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            // use selected item
            if(inventory.equippedTool != null)
                inventory.equippedTool.item.Use(this);
        }

        if (movement != Vector2.zero && !_isMovingToTarget)
        {
            animator.SetFloat("Movement X", movement.x);
            animator.SetFloat("Movement Y", movement.y);
        }
        else if (_isMovingToTarget)
        {
            animator.SetFloat("Movement X", Mathf.Sign(movement.x)); // -1, 0 or 1
            animator.SetFloat("Movement Y", Mathf.Sign(movement.y));
        }
        
        
        
        movement = new Vector2(horizontal, vertical);
        
        _rb.MovePosition(_rb.position + movement * moveSpeed * Time.deltaTime);
    }

    private void MoveToMouse()
    {
        mousePos = Input.mousePosition / 3;
        direction = (mousePos - (Vector3)_rb.position).normalized;
        horizontal = Math.Clamp(direction.x, -1, 1);
        vertical = Math.Clamp(direction.y, -1, 1);
        UpdateAnimations();
    }
}