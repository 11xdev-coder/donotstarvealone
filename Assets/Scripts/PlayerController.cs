using System;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Animator animator;
    
    [Header("Assignable - Important")]
    public Camera mainCamera;
    public AttackableComponent healthComponent;
    public Text hoveringText;
    public GameObject hud;
    
    [Header("Movement")]
    public Vector2 movement;
    public Vector3 mousePos;
    public bool isHit;
    private Rigidbody2D m_Rb;

    [Header("Attacking")]
    public Vector3 targetPos;
    public Vector3 direction;
    public bool moveToMouse;
    public bool canMoveToMouse;
    private bool m_IsMovingToTarget;
    private bool m_IsAttacking;
    private float m_DotProductForward;
    private float m_DotProductRight;
    
    [Header("Animation")]
    public float horizontal;
    public float vertical;
    public bool animLocked;
    public string movementXString;
    public string movementYString;
    public string attackXString;
    public string attackYString;
    
    [Header("Hovering")]
    public Vector3 offset;
    private RaycastHit2D m_Hit;
    
    [Header("Attacking thingies")]
    public GameObject attackTarget;
    
    [Header("Components")]
    public InventoryManager inventory;
    

    private void Awake()
    {
        hud.SetActive(true);
        canMoveToMouse = true;
        m_Rb = GetComponent<Rigidbody2D>();

        if (healthComponent.onDamageTaken != null) healthComponent.onDamageTaken.AddListener(HandleDamage);
        if (healthComponent.onDeath != null) healthComponent.onDeath.AddListener(HandleDeath);
    }

    private void UpdateAnimations()
    {
        if (!animLocked)
        {
            if (movement != Vector2.zero)
            {
                animator.Play("walk");
            }
            else if (m_IsAttacking)
            {
                animator.Play("attack");
            }
            else
            {
                animator.Play("idle");
            }
        }
    }

    #region Animations
    
    public void EndHit() // calls at the end of hit animation
    {
        DisableAnimLock();
        isHit = false;
    }

    public void StopAnimator() // use this func when you want to stop your animation on last frame
    {
        animator.speed = 0f;
    }

    public void DisableAnimLock()
    {
        // disables animLocked bool, use with animations which are not in UpdateAnimations
        animLocked = false;
    }

    public void HandleDamage()
    {
        animator.Play("hit");
        isHit = true;
        animLocked = true;
    }

    public void HandleDeath()
    {
        animator.Play("death");
        animLocked = true;
        hud.SetActive(false);
        RemoveListeners();
        // call everything before we disable controller script
        GetComponent<PlayerController>().enabled = false;
    }

    private void RemoveListeners() // important to call this func when player will be inactive
    {
        if (healthComponent.onDamageTaken != null) healthComponent.onDamageTaken.RemoveListener(HandleDamage);
        if (healthComponent.onDeath != null) healthComponent.onDeath.RemoveListener(HandleDeath);
    }

    public void OnAttackEnd()
    {
        m_IsAttacking = false;
    }
    
    #endregion

    private void MoveTowardsTarget(GameObject target)
    {
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Collider2D selfCollider = GetComponent<Collider2D>();
        
        if (m_IsMovingToTarget)
        {
            // calculating direction
            canMoveToMouse = false;
            targetPos = targetCollider.bounds.center + target.GetComponent<AttackableComponent>().mineOffset; // getting center of the collider and adding offset
            direction = (targetPos - (Vector3)m_Rb.position).normalized;
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
                
                m_IsAttacking = true;
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
        m_IsMovingToTarget = false;
        m_IsAttacking = false;
        attackTarget = null;
    }

    private void Attack(GameObject target)
    {
        Transform rbTransform = m_Rb.transform;
        Vector3 toObjectVector = (target.transform.position - rbTransform.position).normalized;
    
        // Since it's a 2D top-down view game, your forward vector will be along the Y axis
        // Right vector will be along the X axis
        Vector3 playerForward = rbTransform.up;
        Vector3 playerRight = rbTransform.right;

        m_DotProductForward = Vector3.Dot(toObjectVector, playerForward);
        m_DotProductRight = Vector3.Dot(toObjectVector, playerRight);
        
        animator.SetFloat(attackXString, Mathf.Sign(m_DotProductRight));
        animator.SetFloat(attackYString, Mathf.Sign(m_DotProductForward));

        AttackableComponent attackableComponent = target.GetComponent<AttackableComponent>();

        try
        {
            // item in hand
            if(attackableComponent != null)
                attackableComponent.TakeDamage(inventory.equippedTool.item.damage);
        }
        catch
        {
            // nothing in hand
            if(attackableComponent != null && !attackableComponent.isMineable)
                attackableComponent.TakeDamage(5);
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
        if (m_IsMovingToTarget) MoveTowardsTarget(attackTarget); // move

        // if left clicked pressed and we have an attacktarget
        if (Input.GetMouseButtonDown(0) && attackTarget != null) m_IsMovingToTarget = true;
        else if (Input.GetMouseButton(0))
        {
            moveToMouse = true;
            if(canMoveToMouse && !m_IsMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            moveToMouse = false;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            // use selected item
            if(inventory.equippedTool != null)
                inventory.equippedTool.item.Use(this);
        }

        if (movement != Vector2.zero && !m_IsMovingToTarget)
        {
            animator.SetFloat(movementXString, movement.x);
            animator.SetFloat(movementYString, movement.y);
        }
        else if (m_IsMovingToTarget)
        {
            animator.SetFloat(movementXString, Mathf.Sign(movement.x)); // -1, 0 or 1
            animator.SetFloat(movementYString, Mathf.Sign(movement.y));
        }
        
        
        
        movement = new Vector2(horizontal, vertical);
        
        if (!isHit)
        {
            m_Rb.MovePosition(m_Rb.position + movement * moveSpeed * Time.deltaTime); // if not hit - move
        }
        else
        {
            m_Rb.velocity = Vector2.zero; // if hit - stop
        } 
        
        #region Hovering
        
        hoveringText.transform.position = Input.mousePosition + offset;
        // put checks without IsPointerOverInvElement first
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
            if (IsPointerOverComponent<AttackableComponent>())
            {
                if (m_Hit.collider.GetComponent<AttackableComponent>().DoCanAttackCheck(inventory)) // if we can mine the object
                {
                    hoveringText.text = m_Hit.collider.GetComponent<AttackableComponent>().onHoverText; // change the text to one that assigned
                    SetAttackTarget(m_Hit.collider.gameObject); // set attack target and ready to attack
                }
            }
            else
            {
                canMoveToMouse = true;
                hoveringText.text = "Walk";
                if(!m_IsMovingToTarget && !m_IsAttacking) ResetAttackTargetAndMoving();
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

        m_Hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (m_Hit.collider != null)
        {
            if (m_Hit.collider.gameObject.GetComponent<T>())
            {
                return true;
            }
        }

        return false;
    }

    private void MoveToMouse()
    {
        mousePos = Input.mousePosition / 3;
        direction = (mousePos - (Vector3)m_Rb.position).normalized;
        horizontal = Math.Clamp(direction.x, -1, 1);
        vertical = Math.Clamp(direction.y, -1, 1);
        UpdateAnimations();
    }
}