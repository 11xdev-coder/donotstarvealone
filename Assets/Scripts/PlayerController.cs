using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Animator animator;
    
    [Header("Movement")]
    private Rigidbody2D _rb;
    public Vector2 movement;
    public Vector3 mousePos;
    
    [Header("Attacking")]
    public Vector3 targetPos;
    public Vector3 direction;
    public bool moveToMouse;
    public bool canMoveToMouse;
    private bool _isMovingToTarget;
    private bool _isAttacking;
    private float _dotProductForward;
    private float _dotProductRight;
    
    [Header("Animation")]
    public float horizontal;
    public float vertical;
    public bool animLocked;
    
    [Header("Hovering")]
    public Text hoveringText;
    public Vector3 offset;
    private RaycastHit2D _hit;
    
    [Header("Attacking thingies")]
    public GameObject attackTarget;

    public InventoryManager inventory;
    [FormerlySerializedAs("camera")] public Camera mainCamera;

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
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Collider2D selfCollider = GetComponent<Collider2D>();
        
        if (_isMovingToTarget)
        {
            // calculating direction
            canMoveToMouse = false;
            targetPos = targetCollider.bounds.center + target.GetComponent<MineableComponent>().mineOffset; // getting center of the collider and adding offset
            direction = (targetPos - (Vector3)_rb.position).normalized;
            // moving
            horizontal = direction.x;
            vertical = direction.y;
            
            UpdateAnimations();
        }
        
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
        _isMovingToTarget = false;
        _isAttacking = false;
        attackTarget = null;
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
            if(target.GetComponent<HealthComponent>() != null)
                target.GetComponent<HealthComponent>().TakeDamage(inventory.equippedTool.item.damage);
            
            if(target.GetComponent<MineableComponent>() != null)
                target.GetComponent<MineableComponent>().TakeDamage(inventory.equippedTool.item.damage);
        }
        catch
        {
            // nothing in hand
            if(target.GetComponent<HealthComponent>() != null)
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
            moveToMouse = true;
            if(canMoveToMouse && !_isMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            moveToMouse = false;
        
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
        
        #region Hovering
        
        hoveringText.transform.position = Input.mousePosition + offset;
        // put checks without IsPoinerOverInvElement first
        if (inventory.FindClosestSlot() != null && inventory.FindClosestSlot().item != null)
        {
            canMoveToMouse = false;
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = inventory.FindClosestSlot().item.name;
        }
        else if (inventory.IsOverSlot())
        {
            canMoveToMouse = false;
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = "";
        }
        else if (inventory.isMovingItem && !inventory.IsOverSlot())
        {
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = "Drop";
        }
        else if(!inventory.IsOverSlot())
        {
            hoveringText.gameObject.SetActive(true);
            if (IsPointerOverComponent<HealthComponent>())
            {
                hoveringText.text = "Attack";
                SetAttackTarget(_hit.collider.gameObject);
            }
            else if (IsPointerOverComponent<MineableComponent>())
            {
                if (_hit.collider.GetComponent<MineableComponent>().DoCanMineCheck(inventory)) // if we can mine the object
                {
                    hoveringText.text = _hit.collider.GetComponent<MineableComponent>().onHoverText; // change the text to one that assigned
                    SetAttackTarget(_hit.collider.gameObject); // set attack target and ready to attack
                }
            }
            else
            {
                canMoveToMouse = true;
                hoveringText.text = "Walk";
                if(!_isMovingToTarget && !_isAttacking) ResetAttackTargetAndMoving();
            }
                
        }
        else
        {
            hoveringText.gameObject.SetActive(false);
        }
        
        #endregion
    }
    
    private bool IsPointerOverComponent<T>() where T : Component
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z; // This value should be adjusted depending on the positions of your game objects

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePosition);

        _hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (_hit.collider != null)
        {
            if (_hit.collider.gameObject.GetComponent<T>())
            {
                return true;
            }
        }

        return false;
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