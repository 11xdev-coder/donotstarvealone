using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Animator animator;

    [Header("-- Debug --")] 
    public GameObject attackTarget;
    public GameObject currentAttackableTarget;
    public GameObject currentInteractableSlot;
    public GameObject console;
    public bool isConsoleOpen;
    public KeyBindingManager keyBindingManager;
    public RaycastHit2D[] Hits = new RaycastHit2D[5];
    private RaycastHit2D m_Hit;

    [Header("-- Assignable - Important --")]
    public Camera mainCamera;
    public TMP_Text hoveringText;
    public GameObject hud;
    public InventoryManager inventory;
    public GameObject handToolSpriteRenderer;
    
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
    public Vector3 targetPosition;
    public bool isHit;
    private Rigidbody2D m_Rb;
    
    [Header("Dropping")]
    public Vector2 directionDifference;
    public bool isDropping;
    public float dropStopThreshold = 0.2f;
    
    [Header("Mouse")]
    public Vector3 direction;
    public bool isMovingToMouse;
    public bool canMoveToMouse;
    public float minCursorDistance = 0.1f;
    public float bufferCursorDistance = 0.2f;

    [Header("Attacking")] 
    public float searchRadius = 6f;
    public float playerAttackDetectionRadius = 0.3f;
    public Vector3 attackDetectionOffset;
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
        keyBindingManager = FindObjectOfType<KeyBindingManager>().instance;
        
        console = FindObjectOfType<ConsoleManager>().gameObject;
        console.SetActive(false);
        
        hud.SetActive(true);
        canMoveToMouse = true;
        m_Rb = GetComponent<Rigidbody2D>();
        healthFillableImage.fillAmount = 1f;

        AddListeners();
        
        ChangeHandToolSprite(inventory.equippedTool.item);
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
        ChangeHandToolSprite(null);
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

    private void ChangeHandToolSprite(ItemClass tool)
    {
        if (tool != null)
        {
            handToolSpriteRenderer.GetComponent<SpriteRenderer>().sprite = tool.itemSprite;
            handToolSpriteRenderer.transform.localScale = tool.handScale;
        }
        else if (tool == null) handToolSpriteRenderer.GetComponent<SpriteRenderer>().sprite = null;
    }
    
    private void HandleToolChange(ItemClass newTool)
    {
        ChangeHandToolSprite(newTool);
        ResetAttackTargetAndMoving(); // stop moving to target
    }


    private void MoveTowardsTarget(GameObject target, bool doAttack, bool pickUp, bool drop)
    {

        if (!drop)
        {
            Collider2D targetCollider = target.GetComponent<Collider2D>();
        
            if (m_IsMovingToTarget)
            {
                // calculating direction
                canMoveToMouse = false;
                isMovingToMouse = false;
                if (doAttack)
                {
                    targetPosition = targetCollider.bounds.center +
                                     target.GetComponent<AttackableComponent>()
                                         .attackOffset; // getting center of the collider and adding offset
                }

                if (pickUp)
                {
                    targetPosition = targetCollider.bounds.center + target.GetComponent<DroppedItem>().pickupOffset;
                }

                var position = m_Rb.position;
                direction = (targetPosition - (Vector3) position).normalized;
                directionDifference = targetPosition - (Vector3)position;
                // moving
                direction = directionDifference.normalized;
                horizontal = direction.x;
                vertical = direction.y;

                UpdateAnimations();
            }

            // Check if target's collider is within the character's interaction radius using OverlapCircleAll
            Collider2D[] collidersWithinRadius = Physics2D.OverlapCircleAll((Vector3)m_Rb.position + attackDetectionOffset, playerAttackDetectionRadius);
            bool targetIsWithinRadius;
            targetIsWithinRadius = doAttack ? 
                collidersWithinRadius.Any(col => col.gameObject == target && !col.isTrigger) :
                collidersWithinRadius.Any(col => col.gameObject == target);

            if (targetIsWithinRadius)
            {
                ResetAttackTargetAndMoving();
                horizontal = 0;
                vertical = 0;
            
                if (doAttack)
                {
                    m_IsAttacking = true;
                    Attack(target);
                }

                if (pickUp)
                {
                    animLocked = true;
                    animator.Play("Drop");
                    PickUpTargetItem(target);
                }
            }
        }
        else
        {
            // move to the target position since we set it in Hovering
            canMoveToMouse = false;
            isMovingToMouse = false;

            var position = m_Rb.position;
            direction = (targetPosition - (Vector3) position).normalized;
            // moving
            directionDifference = targetPosition - (Vector3)position;
            if (directionDifference.sqrMagnitude < dropStopThreshold * dropStopThreshold)
            {
                // Stop moving
                horizontal = 0;
                vertical = 0;
                isDropping = false;
                inventory.movingSlot.item.DropItem(inventory.movingSlot, transform, inventory);
                animLocked = true;
                animator.Play("Drop");
            }
            else
            {
                direction = directionDifference.normalized;
                horizontal = direction.x;
                vertical = direction.y;
            }

            UpdateAnimations();
        }
    }

    public void PickUpTargetItem(GameObject target)
    {
        DroppedItem droppedItem = target.GetComponent<DroppedItem>();
        if (inventory.Add(droppedItem.item, droppedItem.count)) // Add the item to the inventory (adjust according to your inventory system)
        {
            Destroy(droppedItem.gameObject); // Destroy the dropped item game object
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
        isDropping = false;
        m_IsAttacking = false;
        attackTarget = null;
    }

    private GameObject FindNearestTarget(bool isSpaceHitted, bool isFHitted)
    {
        // Fetch all colliders within the search radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll((Vector3)m_Rb.position + attackDetectionOffset, searchRadius);

        GameObject nearestTarget = null;
        float shortestDistance = Mathf.Infinity;

        // Iterate through all fetched colliders
        foreach (var searchingCollider in colliders)
        {
            if (searchingCollider.gameObject == healthComponent.self) // skip self
                continue;
            
            if (isFHitted)
            {
                AttackableComponent attackableComponent = searchingCollider.GetComponent<AttackableComponent>();
                if (attackableComponent && attackableComponent.DoCanAttackCheck(inventory) && !attackableComponent.isMineable)
                {
                    float distanceToTarget = Vector2.Distance(transform.position, searchingCollider.transform.position);
                    if (distanceToTarget < shortestDistance)
                    {
                        nearestTarget = searchingCollider.gameObject;
                        shortestDistance = distanceToTarget;
                    }
                }
            }
            else if (isSpaceHitted)
            {
                AttackableComponent attackableComponent = searchingCollider.GetComponent<AttackableComponent>();
                if (searchingCollider.GetComponent<DroppedItem>() || (attackableComponent && attackableComponent.isMineable && attackableComponent.DoCanAttackCheck(inventory)))
                {
                    float distanceToTarget = Vector2.Distance(transform.position, searchingCollider.transform.position);
                    if (distanceToTarget < shortestDistance)
                    {
                        nearestTarget = searchingCollider.gameObject;
                        shortestDistance = distanceToTarget;
                    }
                }
            }   
        }
    
        return nearestTarget;
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

        currentAttackableTarget = target;
    }

    public void DealDamage() // call this in animation
    {
        AttackableComponent attackableComponent = currentAttackableTarget.GetComponent<AttackableComponent>();
        try
        {
            // item in hand
            if(attackableComponent != null && currentAttackableTarget != healthComponent.self)
                attackableComponent.TakeDamage(inventory.equippedTool.item.damage);
        }
        catch
        {
            // nothing in hand
            if(attackableComponent != null && !attackableComponent.isMineable && currentAttackableTarget != healthComponent.self)
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
        if (!keyBindingManager.isWaitingForKeyPress && 
            Input.GetKeyUp(keyBindingManager.bindings.OpenConsole))
        {
            if (console.activeSelf)
            {
                console.SetActive(false);
                Time.timeScale = 1f; // Resume the game
            }
            else
            {
                console.SetActive(true);
                Time.timeScale = 0f; // Freeze the game
            }
        }
        
        HealthChanges();

        if (Time.timeScale == 0f) // Game is frozen
        {
            return; // Early exit from the Update method
        }
        
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;
        
        if (Input.GetKey(KeyCode.W))
        {
            vertical = 1;
            ResetAttackTargetAndMoving();
            if (!isHit) DisableAnimLock();
        }

        if (Input.GetKey(KeyCode.S))
        {
            vertical = -1;
            ResetAttackTargetAndMoving();
            if (!isHit) DisableAnimLock();
        }

        if (Input.GetKey(KeyCode.A))
        {
            horizontal = -1;
            ResetAttackTargetAndMoving();
            if (!isHit) DisableAnimLock();
        }

        if (Input.GetKey(KeyCode.D))
        {
            horizontal = 1;
            ResetAttackTargetAndMoving();
            if (!isHit) DisableAnimLock();
        }

        UpdateAnimations();

        // if we want to move to target
        if (m_IsMovingToTarget)
        {
            if (attackTarget.GetComponent<AttackableComponent>())
                MoveTowardsTarget(attackTarget, true, false, false); // attack
            else if (attackTarget.GetComponent<DroppedItem>())
                MoveTowardsTarget(attackTarget, false, true, false); // pick up
        }

        if (isDropping)
        {
            MoveTowardsTarget(attackTarget, false, false, true);
        }


        if (Input.GetKey(KeyCode.F) && !m_IsAttacking) // if smacked F
        {
            GameObject target = FindNearestTarget(false, true);
            if (target != null)
            {
                SetAttackTarget(target); // set our current target to nearest one
                m_IsMovingToTarget = true; // actually move to target
            }
        }
        else if (Input.GetKey(KeyCode.Space) && !m_IsAttacking) // if smacked space bar
        {
            GameObject target = FindNearestTarget(true, false);
            if (target != null)
            {
                SetAttackTarget(target); // set our current target to nearest one
                m_IsMovingToTarget = true; // actually move to target
            }
        }

        if (Input.GetMouseButton(0)) // if we left clicked and no target - move to mouse
        {
            if (attackTarget != null) m_IsMovingToTarget = true;
            else if (canMoveToMouse && !m_IsMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            isMovingToMouse = false;


        movement = new Vector2(horizontal, vertical);

        if (!isHit) // if not hit
        {
            if (!isMovingToMouse) // if not moving to mouse
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
            ResetAttackTargetAndMoving();
            m_Rb.velocity = Vector2.zero; // if hit - stop
        } 
        
        #region Hovering
        
        hoveringText.transform.position = Input.mousePosition + offset;

        if (!EventSystem.current.IsPointerOverGameObject()) // if not hovering over the button (can be annoying)
        {
            if (inventory.FindClosestSlotItem() != null && inventory.FindClosestSlotItem().item != null)
            {
                canMoveToMouse = false;
                hoveringText.gameObject.SetActive(true);
                hoveringText.text = inventory.FindClosestSlotItem().item.name;
                
                ItemClass hoveredItem = inventory.FindClosestSlotItem().item;
                
                if(hoveredItem != null)
                {
                    var itemInfo = hoveredItem.GetDisplayInfo();
                    hoveringText.text = string.Join("\n", itemInfo);
                }
                
                ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
            }
            else if (inventory.IsOverSlot())
            {
                canMoveToMouse = false;
                hoveringText.gameObject.SetActive(true);
                hoveringText.text = "";
                
                ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
            }
            else if (inventory.isMovingItem && !inventory.IsOverSlot())
            {
                hoveringText.gameObject.SetActive(true);
                hoveringText.text = "Drop";
                if (Input.GetMouseButtonDown(0))
                {
                    // Get the mouse position in screen space, then convert to world space
                    Vector3 screenPos = Input.mousePosition;
                    screenPos.z = Mathf.Abs(mainCamera.transform.position.z);
                    targetPosition = mainCamera.ScreenToWorldPoint(screenPos);

                    // Reset the Z coordinate to match your 2D world
                    targetPosition.z = 1;
                    isDropping = true;
                }
                
                ChangeCurrentSlotScale(false, null);
            }
            else if(!inventory.IsOverSlot())
            {
                hoveringText.gameObject.SetActive(true);
                if (IsPointerOverComponent<AttackableComponent>())
                {
                    if (m_Hit.collider.GetComponent<AttackableComponent>().DoCanAttackCheck(inventory)) // if we can mine the object
                    {
                        hoveringText.text = m_Hit.collider.GetComponent<AttackableComponent>().onHoverText; // change the text to one that assigned
                        if(Input.GetMouseButtonDown(0) && !m_IsAttacking) SetAttackTarget(m_Hit.collider.gameObject); // set attack target and ready to attack
                    }
                }
                else if (IsPointerOverComponent<DroppedItem>())
                {
                    hoveringText.gameObject.SetActive(true);
                    hoveringText.text = "Pick up";
                    if (Input.GetMouseButtonDown(0) && !m_IsAttacking) // If the player clicks while hovering over a dropped item
                    {
                        SetAttackTarget(m_Hit.collider.gameObject);
                    }
                }
                else
                {
                    canMoveToMouse = true;
                    hoveringText.text = "Walk";
                    if(!m_IsMovingToTarget && !m_IsAttacking) ResetAttackTargetAndMoving();
                }
                
                ChangeCurrentSlotScale(false, null);
            }
            else
            {
                hoveringText.gameObject.SetActive(false);
            }
        }
        else
        {
            canMoveToMouse = false;
            hoveringText.gameObject.SetActive(false);
        }
        
        #endregion
    }
    
    #region FUNCS FOR HOVERING

    private void ChangeCurrentSlotScale(bool turnOn, Vector3? bigScale)
    {
        if (turnOn)
        {
            try
            {
                currentInteractableSlot.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            catch
            {
                // ignored
            }

            currentInteractableSlot = inventory.FindClosestSlotObject();
            if (bigScale != null) currentInteractableSlot.transform.localScale = (Vector3) bigScale;
        }
        else
        {
            if (currentInteractableSlot != null)
            {
                currentInteractableSlot.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }

    private bool IsPointerOverComponent<T>() where T : Component
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z; // This value should be adjusted depending on the positions of your game objects

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePosition);

        int numHits = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, Hits);

        for (int i = 0; i < numHits; i++)
        {
            RaycastHit2D hit = Hits[i];
            if (hit.collider != null && hit.collider.gameObject != healthComponent.self && hit.collider.gameObject.GetComponent<T>() != null)
            {
                m_Hit = hit; // Store the hit result that you care about
                return true;
            }
        }
        return false;
    }
    
    #endregion

    private void MoveToMouse()
    {
        isMovingToMouse = true;
        
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