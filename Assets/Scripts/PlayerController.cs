using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Animator animator;
    
    [Header("-- Debug --")]
    public GameObject attackTarget;
    private RaycastHit2D m_Hit;
    
    [Header("-- Assignable - Important --")]
    public Camera mainCamera;
    public Text hoveringText;
    public GameObject hud;
    public InventoryManager inventory;
    
    [Header("-- Health - Important --")]
    public AttackableComponent healthComponent;
    public Image healthFillableImage;
    public TMP_Text healthAmountText;
    public TMP_Text maxHealthAmountText;
    public Image heartImage;
    public Sprite heartImageFull;
    public Sprite heartImageCracked;

    [Header("Movement")]
    public float horizontal;
    public float vertical;
    public Vector2 movement;
    public Vector3 mousePos;
    public bool isHit;
    private Rigidbody2D m_Rb;
    
    [Header("Mouse")]
    public Vector3 direction;
    public bool moveToMouse;
    public bool canMoveToMouse;
    public float minCursorDistance = 0.1f;
    public float bufferCursorDistance = 0.2f;

    [Header("Attacking")]
    public Vector3 targetPos;
    private bool m_IsMovingToTarget;
    private bool m_IsAttacking;
    private float m_DotProductForward;
    private float m_DotProductRight;
    
    [Header("Animation")]
    public bool animLocked;
    public string movementXString;
    public string movementYString;
    public string attackXString;
    public string attackYString;
    
    [Header("Hovering")]
    public Vector3 offset;

    private void Awake()
    {
        hud.SetActive(true);
        canMoveToMouse = true;
        m_Rb = GetComponent<Rigidbody2D>();
        healthFillableImage.fillAmount = 1f;

        AddListeners();
    }

    private void UpdateAnimations()
    {
        if (!animLocked)
        {
            if (movement != Vector2.zero)
            {
                animator.SetFloat(movementXString, Math.Sign(Mathf.Round(movement.x))); // -1, 0 or 1
                animator.SetFloat(movementYString, Math.Sign(Mathf.Round(movement.y)));
                animator.Play("walk");
            }
            else if (m_IsAttacking)
            {
                animator.SetFloat(attackXString, Math.Sign(m_DotProductRight));
                animator.SetFloat(attackYString, Math.Sign(m_DotProductForward));
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

    private void DisableAnimLock()
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
    
    public void OnAttackEnd()
    {
        m_IsAttacking = false;
    }
    
    #endregion

    private void AddListeners()
    {
        if (healthComponent.onDamageTaken != null) healthComponent.onDamageTaken.AddListener(HandleDamage);
        if (healthComponent.onDeath != null) healthComponent.onDeath.AddListener(HandleDeath);
        inventory.OnToolChanged += HandleToolChange;
    }

    private void RemoveListeners() // important to call this func when player will be inactive
    {
        if (healthComponent.onDamageTaken != null) healthComponent.onDamageTaken.RemoveListener(HandleDamage);
        if (healthComponent.onDeath != null) healthComponent.onDeath.RemoveListener(HandleDeath);
        inventory.OnToolChanged -= HandleToolChange;
    }

    private void HandleToolChange(ItemClass newTool)
    {
        ResetAttackTargetAndMoving(); // stop moving to target
    }


    private void MoveTowardsTarget(GameObject target)
    {
        Collider2D targetCollider = target.GetComponent<Collider2D>();
        Collider2D selfCollider = GetComponent<Collider2D>();
        
        if (m_IsMovingToTarget)
        {
            // calculating direction
            canMoveToMouse = false;
            moveToMouse = false;
            targetPos = targetCollider.bounds.center + target.GetComponent<AttackableComponent>().attackOffset; // getting center of the collider and adding offset
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
        
        

        AttackableComponent attackableComponent = target.GetComponent<AttackableComponent>();

        try
        {
            // item in hand
            if(attackableComponent != null && target != healthComponent.self)
                attackableComponent.TakeDamage(inventory.equippedTool.item.damage);
        }
        catch
        {
            // nothing in hand
            if(attackableComponent != null && !attackableComponent.isMineable && target != healthComponent.self)
                attackableComponent.TakeDamage(5);
        }
    }

    private void HealthChanges()
    {
        healthFillableImage.fillAmount = (float) healthComponent.health / healthComponent.maxHealth; // fill health sprite depending on health
        healthAmountText.text = Convert.ToString(healthComponent.health); // change text to our hp
        maxHealthAmountText.text = "Max:\n " + Convert.ToString(healthComponent.maxHealth);
        heartImage.sprite = healthComponent.health <= healthComponent.maxHealth / 2 ? heartImageCracked : heartImageFull;
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;
        
        HealthChanges();

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

        // if we have an attack target - move to target
        
        if (Input.GetMouseButton(0)) // if we left clicked and no target - move to mouse
        {
            if (attackTarget != null) m_IsMovingToTarget = true;
            else if (canMoveToMouse && !m_IsMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            moveToMouse = false;

        
        movement = new Vector2(horizontal, vertical);
        
        if (!isHit) // if not hit
        {
            if (!moveToMouse) // if not moving to mouse
            {
                // Normalize the vector if it's length is greater than 1 (this is when diagonal movement occurs)
                if (movement.sqrMagnitude > 1)
                {
                    movement.Normalize();
                }
        
                m_Rb.velocity = movement * moveSpeed; 
            }
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
                    if(Input.GetMouseButtonDown(0)) SetAttackTarget(m_Hit.collider.gameObject); // set attack target and ready to attack
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
        moveToMouse = true;
        
        // Get the mouse position in screen space, then convert to world space
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        mousePos = mainCamera.ScreenToWorldPoint(screenPos);

        // Reset the Z coordinate to match your 2D world
        mousePos.z = 1;
        
        float distance = Vector2.Distance(mousePos, m_Rb.position);
        
        // calculating direction
        direction = ((Vector2)mousePos - m_Rb.position).normalized;
        horizontal = direction.x;
        vertical = direction.y; // setting up these variables for animations to work
        
        // moving
        if (distance > bufferCursorDistance) // if cursor is far away
        {
            m_Rb.velocity = direction * moveSpeed; // simply move
        }
        else if (distance > minCursorDistance) // if cursor is in buffer distance
        {
            // Interpolate velocity from full to zero within the buffer zone
            float bufferFraction = (distance - minCursorDistance) / (bufferCursorDistance - minCursorDistance);
            m_Rb.velocity = direction * (moveSpeed * bufferFraction);
        }
        else // else (if cursor is in minimum distance) dont move
        {
            m_Rb.velocity = Vector2.zero;
            horizontal = 0f;
            vertical = 0f;
        }
        
        UpdateAnimations();
    }
}