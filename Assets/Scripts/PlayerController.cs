using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Animator animator;

    [Header("-- Debug --")] 
    public GameObject attackTarget;
    public GameObject currentAttackableTarget;
    public GameObject currentDroppingTarget;
    public GameObject currentInteractableSlot;
    public CraftingRecipeClass currentCraftable;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[5];
    private GameObject _console;
    private KeyBindingManager _keyBindingManager;
    private GameObject _hit;
    
    [Header("-- Assignable - Important --")]
    public Camera mainCamera;
    public TMP_Text hoveringText;
    public GameObject hud;
    public InventoryManager inventory;
    public GameObject handToolSpriteRenderer;
    private TalkerComponent _talker;
    
    [Header("-- Health - Important --")]
    public AttackableComponent healthComponent;
    public Image healthFillableImage;
    public TMP_Text healthAmountText;
    public TMP_Text maxHealthAmountText;
    public Image heartImage;
    public Sprite heartImageFull;
    public Sprite heartImageCracked;

    [Header("-- Body Elements - Important --")]
    public GameObject head;
    public GameObject body;
    public GameObject armL;
    public GameObject armR;
    public GameObject legL;
    public GameObject legR;
    private List<GameObject> _bodyParts = new List<GameObject>();
    private int _originalSortingOrder;

    [Header("Interactions - Global")] 
    public Settings currentInteractionSettings;
    
    [Header("Interactions - Car")]
    public bool isInCar;
    public GameObject car;
    public Rigidbody2D carRb;
    public float carMoveSpeed;
    public float carTurnSpeed;
    public RectTransform boostPointer;
    public RectTransform boostMarks;
    
    [Header("Movement")]
    public float horizontal;
    public float vertical;
    public Vector2 movement;
    public Vector3 mousePos;
    public Vector3 targetPosition;
    public bool isHit;
    private Rigidbody2D _rb;
    
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
    private bool _isMovingToTarget;
    private bool _isAttacking;
    private float _dotProductForward;
    private float _dotProductRight;
    
    [Header("Animation")]
    public bool animLocked;
    public string movementXString;
    public string movementYString;
    public string attackXString;
    public string attackYString;
    
    [Header("Hovering")]
    public Vector3 hoveringOffset;
    public Vector3 invertedHoveringOffset;
    private bool _isOverSlot;
    private AutoSizeTMP _autoSizeHovering;
    
    private void Awake()
    {
        _keyBindingManager = FindObjectOfType<KeyBindingManager>().instance;
        
        _console = FindObjectOfType<ConsoleManager>().gameObject;
        _console.SetActive(false);
        
        // setting up the body parts
        _bodyParts.Add(body); _bodyParts.Add(head); _bodyParts.Add(armL); _bodyParts.Add(armR); _bodyParts.Add(legL);
        _bodyParts.Add(legR);

        _originalSortingOrder = _bodyParts[0].GetComponent<SpriteRenderer>().sortingOrder;

        
        hud.SetActive(true);
        canMoveToMouse = true;
        _rb = GetComponent<Rigidbody2D>();
        healthFillableImage.fillAmount = 1f;

        _autoSizeHovering = hoveringText.GetComponent<AutoSizeTMP>();
        _talker = GetComponent<TalkerComponent>();

        AddListeners();
        
        ChangeHandToolSprite(inventory.equippedTool.item);
    }

    private void UpdateAnimations()
    {
        if (!animLocked)
        {
            if (_isAttacking)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("attack"))
                {
                    animator.SetFloat(attackXString, Math.Sign(_dotProductRight));
                    animator.SetFloat(attackYString, Math.Sign(_dotProductForward));
                    animator.Play("attack");
                }
            }
            else if (movement != Vector2.zero)
            {
                animator.SetFloat(movementXString, Math.Sign(Mathf.Round(movement.x))); // -1, 0 or 1
                animator.SetFloat(movementYString, Math.Sign(Mathf.Round(movement.y)));
                animator.Play("walk");
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
        ResetAttackTargetAndMoving();
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
        _isAttacking = false;
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

    public void CraftInAnimation()
    {
        if (currentCraftable != null)
        {
            currentCraftable.Craft(inventory);
            isHit = false; // cancels the hit animation
            currentCraftable = null;
        }
    }
    
    public void Craft(CraftingRecipeClass craft)
    {
        if (craft.CanCraft(inventory))
        {
            PlayAnimationAndCancelHit("use");
            currentCraftable = craft;
        }
        else
        {
            if (_talker != null) _talker.Say("Cant craft!");
        }
    }

    private void PlayAnimationAndCancelHit(string anim)
    {
        animLocked = true;
        animator.Play(anim);
        isHit = false;
    }
    
    private void MoveTowardsTarget(GameObject target, bool doAttack, bool pickUp, bool drop)
    {
        if (!drop)
        {
            Collider2D targetCollider = target.GetComponent<Collider2D>();
        
            if (_isMovingToTarget)
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

                var position = _rb.position;
                direction = (targetPosition - (Vector3) position).normalized;
                directionDifference = targetPosition - (Vector3)position;
                // moving
                direction = directionDifference.normalized;
                horizontal = direction.x;
                vertical = direction.y;

                UpdateAnimations();
            }

            // Check if target's collider is within the character's interaction radius using OverlapCircleAll
            Collider2D[] collidersWithinRadius = Physics2D.OverlapCircleAll((Vector3)_rb.position + attackDetectionOffset, playerAttackDetectionRadius);
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
                    _isAttacking = true;
                    Attack(target);
                }

                if (pickUp)
                {
                    PlayAnimationAndCancelHit("Drop");
                    currentDroppingTarget = target;
                    // PickUpTargetItem(target); // we rely on PickUpInAnimation in picking up animations
                }
            }
        }
        else
        {
            // move to the target position since we set it in Hovering
            canMoveToMouse = false;
            isMovingToMouse = false;

            var position = _rb.position;
            direction = (targetPosition - (Vector3) position).normalized;
            // moving
            directionDifference = targetPosition - (Vector3)position;
            if (directionDifference.sqrMagnitude < dropStopThreshold * dropStopThreshold)
            {
                // Stop moving
                horizontal = 0;
                vertical = 0;
                // isDropping = false;
                // inventory.movingSlot.item.DropItem(inventory.movingSlot, transform, inventory); rely onb PickUpInAnimation
                PlayAnimationAndCancelHit("Drop");
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
    
    public void PickUpInAnimation() // call this in animation
    {
        if (isDropping)
        {
            isDropping = false;
            inventory.movingSlot.item.DropItem(inventory.movingSlot, transform, inventory);
        }
        else
        {
            PickUpTargetItem(currentDroppingTarget);
            currentDroppingTarget = null;
            ResetAttackTargetAndMoving();
        }
    }

    public void PickUpTargetItem(GameObject target)
    {
        DroppedItem droppedItem = target.GetComponent<DroppedItem>();
        int remainingItems = inventory.Add(droppedItem.item, droppedItem.count);
    
        // Only decrease the items from the dropped item if they were successfully added to the inventory.
        int successfullyAddedItems = droppedItem.count - remainingItems;
    
        if (successfullyAddedItems > 0)
        {
            droppedItem.count -= successfullyAddedItems;

            if (droppedItem.count <= 0)
            {
                Destroy(droppedItem.gameObject); // Destroy the dropped item game object only if all items were picked up
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
        _isMovingToTarget = false;
        isDropping = false;
        _isAttacking = false;
        attackTarget = null;
    }

    private GameObject FindNearestTarget(bool isSpaceHitted, bool isFHitted)
    {
        // Fetch all colliders within the search radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll((Vector3)_rb.position + attackDetectionOffset, searchRadius);

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
        Transform rbTransform = _rb.transform;
        Vector3 toObjectVector = (target.transform.position - rbTransform.position).normalized;
    
        // Since it's a 2D top-down view game, your forward vector will be along the Y axis
        // Right vector will be along the X axis
        Vector3 playerForward = rbTransform.up;
        Vector3 playerRight = rbTransform.right;

        _dotProductForward = Vector3.Dot(toObjectVector, playerForward);
        _dotProductRight = Vector3.Dot(toObjectVector, playerRight);

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
    
    #region Helper Funcs

    private void AdjustSortingLayers()
    {
        int baseOrder = 200 - Mathf.FloorToInt(transform.position.y);

        body.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
        head.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
        legL.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
        legR.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
        armR.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
        armL.GetComponent<SpriteRenderer>().sortingOrder = baseOrder;
    }

    private void UpdatePointerPosition(Settings settings)
    {
        float bottomYPosition = -(boostMarks.sizeDelta.y / 2); // Bottom is half the height downwards from the center
        float topYPosition = boostMarks.sizeDelta.y / 2;   // Top is half the height upwards from the center
        
        float percentage = settings.CurrentBoost / settings.MaxBoost;
        float newYPosition = Mathf.Lerp(bottomYPosition, topYPosition, percentage);

        boostPointer.anchoredPosition = new Vector2(boostPointer.anchoredPosition.x, newYPosition);
    }

    public void ToggleVisibility(bool toggle)
    {
        if (toggle)
        {
            foreach (GameObject part in _bodyParts)
            {
                part.GetComponent<SpriteRenderer>().enabled = false; // disable sprite renderer for every part
            }
        }
        else
        {
            foreach (GameObject part in _bodyParts)
            {
                part.GetComponent<SpriteRenderer>().enabled = true; // enabling sprite renderer for every part
            }
        }
            
    }

    private void CarLogic()
    {
        gameObject.transform.position = car.transform.position; // effect that we are in car
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        carRb = car.GetComponent<Rigidbody2D>();
        
        print(currentInteractionSettings.CurrentBoost);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            ResetAttackTargetAndMoving();
            if (!isHit) 
                DisableAnimLock();
        }

        float turnSpeed = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);

        // Increase the CurrentBoost based on AccelerationSpeed
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        {
            currentInteractionSettings.CurrentBoost += currentInteractionSettings.AccelerationSpeed * Time.deltaTime;

            // Ensure CurrentBoost doesn't exceed MaxBoost
            currentInteractionSettings.CurrentBoost = Mathf.Min(currentInteractionSettings.CurrentBoost, currentInteractionSettings.MaxBoost);
        }
        else
        {
            // Reset CurrentBoost when not accelerating
            currentInteractionSettings.CurrentBoost = 0;
        }

        if (Input.GetKey(KeyCode.W))
        {
            Vector2 moveDirection = car.transform.up * carMoveSpeed * currentInteractionSettings.CurrentBoost;
            carRb.velocity = moveDirection;

            // If there's a turn input and the car is moving, set rotation
            if(turnSpeed != 0 && carRb.velocity.magnitude > 0.1f)
            {
                float rotationChange = turnSpeed * carTurnSpeed * Time.deltaTime;
                car.transform.Rotate(0, 0, rotationChange);
            }
        }
        else if (Input.GetKey(KeyCode.S)) 
        {
            Vector2 moveDirection = car.transform.up * -carMoveSpeed * currentInteractionSettings.CurrentBoost;
            carRb.velocity = moveDirection;

            // If there's a turn input and the car is moving, set rotation
            if(turnSpeed != 0 && carRb.velocity.magnitude > 0.1f)
            {
                float rotationChange = turnSpeed * carTurnSpeed * Time.deltaTime;
                car.transform.Rotate(0, 0, rotationChange);
            }
        }
        
        UpdatePointerPosition(currentInteractionSettings);
    }

    
    #endregion

    [Obsolete("Obsolete")]
    private void Update()
    {
        if (!_keyBindingManager.isWaitingForKeyPress && 
            Input.GetKeyUp(_keyBindingManager.bindings.OpenConsole))
        {
            if (_console.activeSelf)
            {
                _console.SetActive(false);
                Time.timeScale = 1f; // Resume the game
            }
            else
            {
                _console.SetActive(true);
                Time.timeScale = 0f; // Freeze the game
            }
        }
        
        HealthChanges();
        AdjustSortingLayers();

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
        if (_isMovingToTarget)
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


        if (Input.GetKey(KeyCode.F) && !_isAttacking) // if smacked F
        {
            GameObject target = FindNearestTarget(false, true);
            if (target != null)
            {
                SetAttackTarget(target); // set our current target to nearest one
                _isMovingToTarget = true; // actually move to target
            }
        }
        else if (Input.GetKey(KeyCode.Space) && !_isAttacking) // if smacked space bar
        {
            GameObject target = FindNearestTarget(true, false);
            if (target != null)
            {
                SetAttackTarget(target); // set our current target to nearest one
                _isMovingToTarget = true; // actually move to target
            }
        }

        if (Input.GetMouseButton(0)) // if we left clicked and no target - move to mouse
        {
            if (attackTarget != null) _isMovingToTarget = true;
            else if (canMoveToMouse && !_isMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            isMovingToMouse = false;


        if (isInCar && car != null) // in car
        {
            CarLogic();
        }


        if (!canMoveToMouse) isMovingToMouse = false;


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
        
                _rb.velocity = movement * moveSpeed; 
            }
        }
        else
        {
            ResetAttackTargetAndMoving();
            _rb.velocity = Vector2.zero; // if hit - stop
        } 
        
        #region Hovering

        hoveringText.transform.position = Input.mousePosition + AdjustOffsetForScreenBorders(hoveringOffset, invertedHoveringOffset);

        if (!EventSystem.current.IsPointerOverGameObject()) // if not hovering over the button (can be annoying)
        {
            if (inventory.FindClosestSlotItem() != null && inventory.FindClosestSlotItem().item != null)
            {
                canMoveToMouse = false;
                hoveringText.gameObject.SetActive(true);
                _autoSizeHovering.UpdateText(inventory.FindClosestSlotItem().item.name);

                ItemClass hoveredItem = inventory.FindClosestSlotItem().item;

                if (hoveredItem != null)
                {
                    var itemInfo = hoveredItem.GetDisplayInfo();
                    _autoSizeHovering.UpdateText(String.Join("\n", itemInfo));
                }

                _isOverSlot = true;
                ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
            }
            else if (inventory.IsOverSlot())
            {
                canMoveToMouse = false;
                hoveringText.gameObject.SetActive(true);
                _autoSizeHovering.UpdateText("");
                
                _isOverSlot = true;
                ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
            }
            else if (inventory.isMovingItem && !inventory.IsOverSlot())
            {
                hoveringText.gameObject.SetActive(true);
                _autoSizeHovering.UpdateText("Drop");
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3 screenPos = Input.mousePosition;
                    screenPos.z = Mathf.Abs(mainCamera.transform.position.z);
                    targetPosition = mainCamera.ScreenToWorldPoint(screenPos);

                    targetPosition.z = 1;
                    isDropping = true;
                }

                ChangeCurrentSlotScale(false, null);
            }
            else if (!inventory.IsOverSlot())
            {
                hoveringText.gameObject.SetActive(true);
                if (IsPointerOverComponent<AttackableComponent>())
                {
                    if (_hit.gameObject.GetComponent<AttackableComponent>()
                        .DoCanAttackCheck(inventory)) // if we can mine the object
                    {
                        _autoSizeHovering.UpdateText(_hit.gameObject.GetComponent<AttackableComponent>().onHoverText);
                        if (Input.GetMouseButtonDown(0) && !_isAttacking)
                            SetAttackTarget(_hit.gameObject.gameObject); // set attack target and ready to attack
                    }
                }
                else if (IsPointerOverComponent<DroppedItem>())
                {
                    hoveringText.gameObject.SetActive(true);
                    _autoSizeHovering.UpdateText("Pick up " + _hit.gameObject.GetComponent<DroppedItem>().item.itemName +
                                            " x" +
                                            _hit.gameObject.GetComponent<DroppedItem>().count);
                    if (Input.GetMouseButtonDown(0) &&
                        !_isAttacking) // If the player clicks while hovering over a dropped item
                    {
                        SetAttackTarget(_hit.gameObject.gameObject);
                    }
                }
                else if (IsPointerOverComponent<InteractableComponent>()) // Checking for Interactable component
                {
                    InteractableComponent interactableComponent = _hit.gameObject.GetComponent<InteractableComponent>();
                    hoveringText.gameObject.SetActive(true);
                    _autoSizeHovering.UpdateText("Interact");

                    if (Input.GetMouseButtonDown(0) && !_isAttacking)
                    {
                        SetAttackTarget(_hit.gameObject
                            .gameObject); // Assuming you're using the same mechanism to move to the object as with the attack target
                        interactableComponent
                            .Interact(gameObject); // Execute the action from the Interactable component
                    }
                }
                else
                {
                    canMoveToMouse = true;
                    _autoSizeHovering.UpdateText("Walk");
                    if (!_isMovingToTarget && !_isAttacking) ResetAttackTargetAndMoving();
                }
                
                _isOverSlot = false;
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

    private float DetermineScreenHeightAdjustmentFactor(float bestHeight)
    {
        return 1f; // fix it later
        // float referenceHeight = bestHeight;
        // float currentHeight = Screen.height;
        //
        // float scaleY = currentHeight / referenceHeight;
        //
        // return scaleY;
    }

    private Vector3 CalculateScaledOffset(Vector3 originalOffset)
    {
        // Reference and current resolutions
        float referenceWidth = 1920.0f;
        float referenceHeight = 1080.0f;
        float currentWidth = Screen.width;
        float currentHeight = Screen.height;

        // Calculate scale factors
        float scaleX = currentWidth / referenceWidth;
        float scaleY = currentHeight / referenceHeight;

        // Scale the offset
        return new Vector3(
            originalOffset.x * scaleX,
            originalOffset.y * scaleY,
            originalOffset.z
        );
    }

    private Vector3 AdjustOffsetForScreenBorders(Vector3 originalOffset, Vector3 invertOffset)
    {
        // scaling offsets
        Vector3 scaledOffset = CalculateScaledOffset(originalOffset);
        Vector3 scaledInvertedOffset = CalculateScaledOffset(invertOffset);

        // getting rect transform
        RectTransform textRectTransform = hoveringText.GetComponent<RectTransform>();

        // adjustment factor
        float screenHeightAdjustmentFactor = DetermineScreenHeightAdjustmentFactor(Screen.height);

        // adjusted screen height
        float adjustedScreenHeight = Screen.height * screenHeightAdjustmentFactor;

        // potential position of text's top
        Vector3 potentialTopPosition = Input.mousePosition + new Vector3(scaledOffset.x, textRectTransform.rect.height + scaledOffset.y, scaledOffset.z);

        // invert offset if y position goes beyond the screen and is over slot
        if (potentialTopPosition.y > adjustedScreenHeight && _isOverSlot)
        {
            scaledOffset.y = scaledInvertedOffset.y - (textRectTransform.rect.height / 1.5f - scaledInvertedOffset.y);
        }

        // final pos
        Vector3 finalPosition = Input.mousePosition + CalculateScaledOffset(scaledOffset);

        // clamp
        finalPosition.x = Mathf.Clamp(finalPosition.x, 0, Screen.width);
        finalPosition.y = Mathf.Clamp(finalPosition.y, 0, Screen.height);

        return finalPosition - Input.mousePosition;
    }


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
            if (bigScale != null && currentInteractableSlot != null) currentInteractableSlot.transform.localScale = (Vector3) bigScale;
        }
        else
        {
            if (currentInteractableSlot != null)
            {
                currentInteractableSlot.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }

    [Obsolete("Obsolete")]
    private bool IsPointerOverComponent<T>() where T : Component
    {
        // get mouse position
        Vector3 mouseScreenPosition = Input.mousePosition;

        // Create a ray from the camera through the mouse position
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPosition);

        // Determine the point where this ray intersects with the plane of your game (assuming it's at z=0)
        // This requires calculating the distance along the ray that corresponds to z=0
        float rayDistance;
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero); // Game plane facing forward at origin
        if (gamePlane.Raycast(ray, out rayDistance))
        {
            // Get the intersection point
            Vector3 hitPoint = ray.GetPoint(rayDistance);
            Vector2 hitPoint2D = new Vector2(hitPoint.x, hitPoint.y);

            // raycast and store in _hits
            int hitCount = Physics2D.RaycastNonAlloc(hitPoint2D, Vector2.zero, _hits);
    
            // the nearest values
            RaycastHit2D nearestHit = new RaycastHit2D();
            float nearestDistance = float.MaxValue;

            // find the nearest valid hit
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (hit.collider != null && hit.collider.gameObject != healthComponent.self) // is not self
                {
                    T component = hit.collider.GetComponent<T>();
                    if (component != null)
                    {
                        float distance = Vector2.Distance(hitPoint2D, hit.point);
                        if (distance < nearestDistance)
                        {
                            nearestHit = hit;
                            nearestDistance = distance;
                        }
                    }
                }
            }

            // if a valid nearest hit was found
            if (nearestHit.collider != null)
            {
                _hit = nearestHit.collider.gameObject;
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
        
        float distance = Vector2.Distance(mousePos, _rb.position);
        
        // calculating direction
        direction = ((Vector2)mousePos - _rb.position).normalized;
        horizontal = direction.x;
        vertical = direction.y; // setting up these variables for animations to work
        
        // moving
        if (distance > bufferCursorDistance) // if cursor is far away
        {
            _rb.velocity = direction * moveSpeed; // simply move
        }
        else if (distance > minCursorDistance) // if cursor is in buffer distance
        {
            // Interpolate velocity from full to zero within the buffer zone
            float bufferFraction = (distance - minCursorDistance) / (bufferCursorDistance - minCursorDistance);
            _rb.velocity = direction * (moveSpeed * bufferFraction);
        }
        else // else (if cursor is in minimum distance) dont move
        {
            _rb.velocity = Vector2.zero;
            horizontal = 0f;
            vertical = 0f;
        }
        
        UpdateAnimations();
        ResetAttackTargetAndMoving();
    }
    
    // void OnDrawGizmos()
    // {
    //     // Draw a yellow sphere at the player's position + attackDetectionOffset, with a radius of playerAttackDetectionRadius.
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere((Vector3)_rb.position + attackDetectionOffset, playerAttackDetectionRadius);
    // }

}