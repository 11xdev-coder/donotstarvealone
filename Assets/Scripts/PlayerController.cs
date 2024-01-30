using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Inventory;
using JetBrains.Annotations;
using Mirror;
using Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vector2 = UnityEngine.Vector2;

public class PlayerController : NetworkBehaviour
{
    [SyncVar]
    public float moveSpeed = 2f;
    public Animator animator;
    public GameObject tileIndicator;

    [Header("-- Debug --")] 
    public GameObject attackTarget;
    public GameObject currentAttackableTarget;
    public GameObject currentDroppingTarget;
    public GameObject currentInteractableSlot;
    public CraftingRecipeClass currentCraftable;
    public WorldGenerator.PossibleBiomes currentBiome;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[5];
    public GameObject console;
    //private KeyBindingManager _keyBindingManager;
    private GameObject _hit;
    
    [Header("-- Assignable - Important --")]
    private Camera _mainCamera;
    private TMP_Text _hoveringText;
    private GameObject _hud;
    public InventoryManager inventory;
    public WorldGenerator world;
    public GameObject handToolSpriteRenderer;
    [CanBeNull] public UnityEvent onBiomeChanged;
    private TalkerComponent _talker;
    
    [Header("-- Health - Important --")]
    public AttackableComponent healthComponent;
    private Image _healthFillableImage;
    private TMP_Text _healthAmountText;
    public TMP_Text maxHealthAmountText;
    private Image _heartImage;
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
    [SyncVar] public bool isInCar;
    [SyncVar] public GameObject car;
    public Rigidbody2D carRb;
    [SyncVar] public float carMoveSpeed;
    [SyncVar] public float carTurnSpeed;
    private RectTransform _boostPointer;
    private RectTransform _boostMarks;
    
    [Header("Movement")]
    [SyncVar] public float horizontal;
    [SyncVar] public float vertical;
    [SyncVar] public Vector2 movement;
    public Vector3 mousePos;
    [SyncVar] public Vector3 targetPosition;
    [SyncVar] public bool isHit;
    public Rigidbody2D rb;
    
    [Header("Right Clicking")]
    public bool hasRightClicked;
    public Vector3Int targetTilePosition;
    
    [Header("Dropping")]
    public Vector2 directionDifference;
    [SyncVar] public bool isDropping;
    public float dropStopThreshold = 0.2f;
    
    [Header("Mouse")]
    [SyncVar] public Vector3 direction;
    [SyncVar] public bool isMovingToMouse;
    [SyncVar] public bool canMoveToMouse;
    public float minCursorDistance = 0.1f;
    public float bufferCursorDistance = 0.2f;

    [Header("Attacking")] 
    [SyncVar] public float searchRadius = 6f;
    [SyncVar] public float playerAttackDetectionRadius = 0.3f;
    public Vector3 attackDetectionOffset;
    private bool _isMovingToTarget;
    private bool _isAttacking;
    private float _dotProductForward;
    private float _dotProductRight;
    
    [Header("Animation")]
    [SyncVar] public bool animLocked;
    public string movementXString;
    public string movementYString;
    public string attackXString;
    public string attackYString;
    
    [Header("Hovering")]
    public Vector3 hoveringOffset;
    public Vector3 invertedHoveringOffset;
    private bool _isOverSlot;
    private AutoSizeTMP _autoSizeHovering;

    [Header("Post Processing")] 
    private PostProcessVolume _ppv;
    [Range(0f, 1f)]
    public float ashBiomeEnterSmoothness;
    [Range(0f, 1f)]
    public float ashBiomeExitSmoothness;
    private MusicManager _musicManger;
    
    // ReSharper disable Unity.PerformanceAnalysis
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        enabled = true;
        
        // tag finding
        tileIndicator = TileIndicatorSingleton.Instance;
        
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        
        console = GameObject.FindGameObjectWithTag("Console");
        _hoveringText = GameObject.FindGameObjectWithTag("HoveringText").GetComponent<TMP_Text>();
        _hud = GameObject.FindGameObjectWithTag("HUD");
        
        _healthFillableImage = GameObject.FindGameObjectWithTag("HealthFillable").GetComponent<Image>();
        _healthAmountText = GameObject.FindGameObjectWithTag("HealthAmount").GetComponent<TMP_Text>();
        maxHealthAmountText = MaxHealthSingleton.Instance;
        _healthFillableImage.GetComponent<ShowUIText>().found = true;
        _heartImage = GameObject.FindGameObjectWithTag("HeartImage").GetComponent<Image>();
        // TODO: boostPointer not found
        _boostPointer = GameObject.FindGameObjectWithTag("BoostPointer").GetComponent<RectTransform>();
        _boostPointer.gameObject.SetActive(false);
        _boostMarks = GameObject.FindGameObjectWithTag("BoostMarks").GetComponent<RectTransform>();
        _boostMarks.gameObject.SetActive(false);
        
        _ppv = GameObject.FindGameObjectWithTag("PPV").GetComponent<PostProcessVolume>();

        
        // TODO: console not found
        console = FindFirstObjectByType<ConsoleManager>().gameObject;
        console.SetActive(false);
        
        // setting up the body parts
        _bodyParts.Add(body); _bodyParts.Add(head); _bodyParts.Add(armL); _bodyParts.Add(armR); _bodyParts.Add(legL);
        _bodyParts.Add(legR);

        _originalSortingOrder = _bodyParts[0].GetComponent<SpriteRenderer>().sortingOrder;

        
        _hud.SetActive(true);
        canMoveToMouse = true;
        rb = GetComponent<Rigidbody2D>();
        _healthFillableImage.fillAmount = 1f;

        _autoSizeHovering = _hoveringText.GetComponent<AutoSizeTMP>();
        _talker = GetComponent<TalkerComponent>();
        
        world = WorldGenerator.Instance;
        world.enabled = true;
        transform.position = world.spawnPoint;
        
        _musicManger = FindFirstObjectByType<MusicManager>().Instance;

        AddListeners();
        
        ChangeHandToolSprite(inventory.equippedTool.item);

        StartCoroutine(GetCurrentBiome());
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        enabled = true;
        
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        _mainCamera.GetComponent<CameraFollow>().target = gameObject;
        _mainCamera.GetComponent<CameraFollow>().enabled = true;
        
        transform.position = WorldGenerator.Instance.spawnPoint;
    }
    
    [Command]
    void CmdTriggerAnimation(string animationName)
    {
        RpcPlayAnimation(animationName);
    }

    [ClientRpc]
    void RpcPlayAnimation(string animationName)
    {
        if (!isLocalPlayer) return;
        
        animator.Play(animationName);
    }
    
    private void UpdateAnimations()
    {
        if (!isLocalPlayer) return;
        
        if (!animLocked)
        {
            if (_isAttacking)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("attack"))
                {
                    animator.SetFloat(attackXString, Math.Sign(_dotProductRight));
                    animator.SetFloat(attackYString, Math.Sign(_dotProductForward));
                    CmdTriggerAnimation("attack");
                }
            }
            else if (movement != Vector2.zero)
            {
                animator.SetFloat(movementXString, Math.Sign(Mathf.Round(movement.x))); // -1, 0 or 1
                animator.SetFloat(movementYString, Math.Sign(Mathf.Round(movement.y)));
                CmdTriggerAnimation("walk");
            }
            else 
            {
                CmdTriggerAnimation("idle");
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
        if(isLocalPlayer) CmdTriggerAnimation("hit");
        isHit = true;
        animLocked = true;
        ResetAttackTargetAndMoving();
    }

    public void HandleDeath()
    {
        if(isLocalPlayer) CmdTriggerAnimation("death");
        animLocked = true;
        _hud.SetActive(false);
        ChangeHandToolSprite(null);
        RemoveListeners();
        // call everything before we disable controller script
        GetComponent<PlayerController>().enabled = false;
    }

    public void HandleBiomeChange()
    {
        Vignette vignette;
        
        if (_ppv.profile.TryGetSettings(out vignette))
        {
            if (currentBiome == WorldGenerator.PossibleBiomes.Ash)
            {
                StartCoroutine(world.TransitionValues(vignette.smoothness.value, ashBiomeEnterSmoothness, 1f,
                    finalValue => vignette.smoothness.value = finalValue));
            }
            else
            {
                StartCoroutine(world.TransitionValues(vignette.smoothness.value, ashBiomeExitSmoothness, 1f,
                    finalValue => vignette.smoothness.value = finalValue));
            }
        }
        
        _musicManger.ChangeCurrentMusic(world.GetBiomeByPosition(rb.position).music);
    }
    
    private IEnumerator GetCurrentBiome()
    {
        while (true)
        {
            if (currentBiome != world.GetBiomeByPositionEnum(rb.position))
            {
                currentBiome = world.GetBiomeByPositionEnum(rb.position);
                onBiomeChanged?.Invoke();
            }

            yield return new WaitForSeconds(1f);
        }
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
        if (onBiomeChanged != null) onBiomeChanged.AddListener(HandleBiomeChange);
        inventory.OnToolChanged += HandleToolChange;
    }

    private void RemoveListeners() // important to call this func when player will be inactive
    {
        if (healthComponent.onDamageTaken != null) healthComponent.onDamageTaken.RemoveListener(HandleDamage);
        if (healthComponent.onDeath != null) healthComponent.onDeath.RemoveListener(HandleDeath);
        if (onBiomeChanged != null) onBiomeChanged.RemoveListener(HandleBiomeChange);
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
        if(isLocalPlayer) CmdTriggerAnimation(anim);
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

                var position = rb.position;
                direction = (targetPosition - (Vector3) position).normalized;
                directionDifference = targetPosition - (Vector3)position;
                // moving
                direction = directionDifference.normalized;
                horizontal = direction.x;
                vertical = direction.y;

                UpdateAnimations();
            }

            // Check if target's collider is within the character's interaction radius using OverlapCircleAll
            Collider2D[] collidersWithinRadius = Physics2D.OverlapCircleAll((Vector3)rb.position + attackDetectionOffset, playerAttackDetectionRadius);
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

            var position = rb.position;
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
            if(!hasRightClicked) inventory.movingSlot.item.DropItem(inventory.movingSlot, transform, inventory);
            else if (hasRightClicked && inventory.movingSlot.item.GetTilePlacer() != null) inventory.movingSlot.item.RightClick(this, targetTilePosition);
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
        if (!isServer) return;

        if (isLocalPlayer)
        {
            DroppedItem droppedItem = target.GetComponent<DroppedItem>();
            inventory.RequestAddItem(ItemRegistry.Instance.GetIdByItem(droppedItem.item), droppedItem.count); 
            int remainingItems = inventory.RemainingItems;
    
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
    }
    
    public void SetAttackTarget(GameObject target)
    {
        attackTarget = target;
        hasRightClicked = false;
        canMoveToMouse = false;
    }

    private void ResetAttackTargetAndMoving()
    {
        // reset attack target and set that we can move to mouse
        _isMovingToTarget = false;
        isDropping = false;
        _isAttacking = false;
        attackTarget = null;
        hasRightClicked = false;
    }

    private GameObject FindNearestTarget(bool isSpaceHitted, bool isFHitted)
    {
        // Fetch all colliders within the search radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll((Vector3)rb.position + attackDetectionOffset, searchRadius);

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

    [Command]
    public void CmdSetCurrentAttackableTarget(GameObject target)
    {
        // Validate the target on the server side
        AttackableComponent attackableComponent = null;
        if(target != null) attackableComponent = target.GetComponent<AttackableComponent>();
        if (attackableComponent != null)
        {
            currentAttackableTarget = target;
        }
    }

    public void AttemptToSetAttackTarget(GameObject target)
    {
        if (!isServer) // If we're on the client, send a command to the server
        {
            CmdSetCurrentAttackableTarget(target);
        }
        else // If we're on the server, we can set it directly
        {
            currentAttackableTarget = target;
        }
    }
    
    private void Attack(GameObject target)
    {
        Transform rbTransform = rb.transform;
        Vector3 toObjectVector = (target.transform.position - rbTransform.position).normalized;
    
        // Since it's a 2D top-down view game, your forward vector will be along the Y axis
        // Right vector will be along the X axis
        Vector3 playerForward = rbTransform.up;
        Vector3 playerRight = rbTransform.right;

        _dotProductForward = Vector3.Dot(toObjectVector, playerForward);
        _dotProductRight = Vector3.Dot(toObjectVector, playerRight);

        AttemptToSetAttackTarget(target);
    }
    
    public void DealDamage() // Call this in animation
    {
        if (!isServer) return;
        
        if (currentAttackableTarget == null)
        {
            Debug.LogError("DealDamage: currentAttackableTarget is null");
            return;
        }
        
        AttackableComponent attackableComponent = currentAttackableTarget.GetComponent<AttackableComponent>();
        if (attackableComponent != null && currentAttackableTarget != healthComponent.self)
        {
            ItemClass item = inventory.equippedTool.item;
            print($"DealDamage to {currentAttackableTarget.name}");
            int damage = (item != null) ? item.damage : 2; // Default damage if no tool
            attackableComponent.DealDamage(damage);
        }
        else
        {
            Debug.LogError("DealDamage: AttackableComponent is null or trying to damage self");
        }
    }
    
    private void MoveToTileEdge()
    {
        if (!hasRightClicked) return;
        
        canMoveToMouse = false;
        isMovingToMouse = false;

        var position = rb.position;
        direction = (targetTilePosition - (Vector3) position).normalized;
        // moving
        directionDifference = targetTilePosition - (Vector3)position;
        if (directionDifference.sqrMagnitude < 1f * 1f)
        {
            // Stop moving
            horizontal = 0;
            vertical = 0;
            if (inventory.movingSlot.item != null && inventory.movingSlot.item.GetTilePlacer() != null)
            {
                inventory.movingSlot.item.RightClick(this, targetTilePosition);
                hasRightClicked = false;
            }
        }
        else
        {
            direction = directionDifference.normalized;
            horizontal = direction.x;
            vertical = direction.y;
        }
    }
    
    private void HealthChanges()
    {
        if (maxHealthAmountText == null)
        {
            maxHealthAmountText = MaxHealthSingleton.Instance;
            if (maxHealthAmountText) Debug.LogWarning("maxHealthAmountText not found");
        }
        _healthFillableImage.fillAmount = (float) healthComponent.health / healthComponent.maxHealth; // fill health sprite depending on health
        _healthAmountText.text = Convert.ToString(healthComponent.health); // change text to our hp
        maxHealthAmountText.text = "Max:\n " + Convert.ToString(healthComponent.maxHealth);
        _heartImage.sprite = healthComponent.health <= healthComponent.maxHealth / 2 ? heartImageCracked : heartImageFull;
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
        float bottomYPosition = -(_boostMarks.sizeDelta.y / 2); // Bottom is half the height downwards from the center
        float topYPosition = _boostMarks.sizeDelta.y / 2;   // Top is half the height upwards from the center
        
        float percentage = settings.CurrentBoost / settings.MaxBoost;
        float newYPosition = Mathf.Lerp(bottomYPosition, topYPosition, percentage);

        _boostPointer.anchoredPosition = new Vector2(_boostPointer.anchoredPosition.x, newYPosition);
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

    private void HandleLocalInput()
    {
        // horizontal = 0f;
        // vertical = 0f;
        //
        // //  basic movement ------------------------
        // if (Input.GetKey(KeyCode.W))
        // {
        //     vertical = 1;
        //     ResetAttackTargetAndMoving();
        //     if (!isHit) DisableAnimLock();
        // }
        //
        // if (Input.GetKey(KeyCode.S))
        // {
        //     vertical = -1;
        //     ResetAttackTargetAndMoving();
        //     if (!isHit) DisableAnimLock();
        // }
        //
        // if (Input.GetKey(KeyCode.A))
        // {
        //     horizontal = -1;
        //     ResetAttackTargetAndMoving();
        //     if (!isHit) DisableAnimLock();
        // }
        //
        // if (Input.GetKey(KeyCode.D))
        // {
        //     horizontal = 1;
        //     ResetAttackTargetAndMoving();
        //     if (!isHit) DisableAnimLock();
        // }
    }

    private void HandleLocalMovement()
    {
        // movement = new Vector2(horizontal, vertical);
        //
        // if (!isHit) // if not hit
        // {
        //     if (!isMovingToMouse) // if not moving to mouse
        //     {
        //         // Normalize the vector if it's length is greater than 1 (this is when diagonal movement occurs)
        //         if (movement.sqrMagnitude > 1)
        //         {
        //             movement.Normalize();
        //         }
        //
        //         rb.velocity = movement * moveSpeed; 
        //     }
        // }
        // else
        // {
        //     ResetAttackTargetAndMoving();
        //     rb.velocity = Vector2.zero; // if hit - stop
        // } 
        //
        // if (isInCar && car != null) // in car
        // {
        //     CarLogic();
        // }
    }
    
    private void HandleLocalInteractions()
    {
        // // attacking & mining ---------------------------------------
        // if (Input.GetKey(KeyCode.F) && !_isAttacking) // if smacked F
        // {
        //     GameObject target = FindNearestTarget(false, true);
        //     if (target != null)
        //     {
        //         SetAttackTarget(target); // set our current target to nearest one
        //         _isMovingToTarget = true; // actually move to target
        //     }
        // }
        // else if (Input.GetKey(KeyCode.Space) && !_isAttacking) // if smacked space bar
        // {
        //     GameObject target = FindNearestTarget(true, false);
        //     if (target != null)
        //     {
        //         SetAttackTarget(target); // set our current target to nearest one
        //         _isMovingToTarget = true; // actually move to target
        //     }
        // }
        //
        // // clicking -------------------------------------------------------------
        // if (Input.GetMouseButton(0)) // if we left clicked and no target - move to mouse
        // {
        //     if (attackTarget != null) _isMovingToTarget = true;
        //     else if (canMoveToMouse && !_isMovingToTarget) MoveToMouse();
        // }
        // else if (Input.GetMouseButtonUp(0))
        //     isMovingToMouse = false;
        //
        //
        // // if we want to move to target
        // if (_isMovingToTarget)
        // {
        //     if (attackTarget.GetComponent<AttackableComponent>())
        //         MoveTowardsTarget(attackTarget, true, false, false); // attack
        //     else if (attackTarget.GetComponent<DroppedItem>())
        //         MoveTowardsTarget(attackTarget, false, true, false); // pick up
        // }
        //
        // if (isDropping)
        // {
        //     MoveTowardsTarget(attackTarget, false, false, true);
        // }
        //
        // if (hasRightClicked)
        // {
        //     MoveToTileEdge();
        // }
        //
        // if (!canMoveToMouse) isMovingToMouse = false;
    }

    private void CommonUpdate()
    {
        // if(_mainCamera == null) {
        //     _mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        //     if(_mainCamera == null) {
        //         Debug.LogWarning("Main camera not found");
        //     }
        // }
        //
        // if(world == null) {
        //     world = WorldGenerator.Instance;
        //     if(world == null) {
        //         Debug.LogWarning("WorldGenerator instance not found");
        //     }
        // }
        //
        // if (tileIndicator == null)
        // {
        //     tileIndicator = TileIndicatorSingleton.Instance;
        //     if (tileIndicator == null) Debug.LogWarning("_tileIndicator not found");
        // }
        //
        // if (_hoveringText == null)
        // {
        //     _hoveringText = GameObject.FindGameObjectWithTag("HoveringText").GetComponent<TMP_Text>();
        //     if (_hoveringText == null) Debug.LogWarning("_hoveringText not found");
        // }
        //
        // if (_autoSizeHovering == null)
        // {
        //     _autoSizeHovering = _hoveringText.GetComponent<AutoSizeTMP>();
        //     if (_autoSizeHovering == null) Debug.LogWarning("_autoSizeHovering not found");
        // }

        
        // mousePos = Input.mousePosition;
        // mousePos.z = Mathf.Abs(_mainCamera.transform.position.z);
        // Vector3 mouseWorldPoint = _mainCamera.ScreenToWorldPoint(mousePos);
        //
        // // Ensure the z-coordinate is set appropriately
        // mouseWorldPoint.z = 1;
        //
        // // Round to the nearest whole number to get the tile position
        // Vector3Int intPosition = world.ClampVector3(mouseWorldPoint);
        //
        // Vector3Int tilePosition = world.triggerTilemap.WorldToCell(intPosition);
        // tileIndicator.transform.position = tilePosition;
        //
        // if (inventory.movingSlot.item != null && inventory.movingSlot.item.GetTilePlacer() != null)
        // {
        //     tileIndicator.SetActive(true);
        // }
        // else
        // {
        //     tileIndicator.SetActive(false);
        // }
        

        
        // if (!KeyBindingManager.Instance.isWaitingForKeyPress && 
        //     Input.GetKeyUp(KeyBindingManager.Instance.bindings.OpenConsole))
        // {
        //     if (console.activeSelf)
        //     {
        //         console.SetActive(false);
        //         Time.timeScale = 1f; // Resume the game
        //     }
        //     else
        //     {
        //         console.SetActive(true);
        //         Time.timeScale = 0f; // Freeze the game
        //     }
        // }
        
        // HealthChanges();
        // AdjustSortingLayers();
        // UpdateAnimations();
        
        // #region Hovering
        //
        // _hoveringText.transform.position = Input.mousePosition + AdjustOffsetForScreenBorders(hoveringOffset, invertedHoveringOffset);
        //
        // if (!EventSystem.current.IsPointerOverGameObject()) // if not hovering over the button (can be annoying)
        // {
        //     if (inventory.FindClosestSlotItem() != null && inventory.FindClosestSlotItem().item != null)
        //     {
        //         canMoveToMouse = false;
        //         _hoveringText.gameObject.SetActive(true);
        //         _autoSizeHovering.UpdateText(inventory.FindClosestSlotItem().item.name);
        //
        //         ItemClass hoveredItem = inventory.FindClosestSlotItem().item;
        //
        //         if (hoveredItem != null)
        //         {
        //             var itemInfo = hoveredItem.GetDisplayInfo();
        //             _autoSizeHovering.UpdateText(String.Join("\n", itemInfo));
        //         }
        //
        //         _isOverSlot = true;
        //         ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
        //     }
        //     else if (inventory.IsOverSlot())
        //     {
        //         canMoveToMouse = false;
        //         _hoveringText.gameObject.SetActive(true);
        //         _autoSizeHovering.UpdateText("");
        //         
        //         _isOverSlot = true;
        //         ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
        //     }
        //     else if (inventory.isMovingItem && !inventory.IsOverSlot())
        //     {
        //         _hoveringText.gameObject.SetActive(true);
        //         _autoSizeHovering.UpdateText("Drop");
        //         if (Input.GetMouseButtonDown(0)) // if left clicked
        //         {
        //             Vector3 screenPos = Input.mousePosition;
        //             screenPos.z = Mathf.Abs(_mainCamera.transform.position.z);
        //             targetPosition = _mainCamera.ScreenToWorldPoint(screenPos);
        //
        //             targetPosition.z = 1;
        //             isDropping = true;
        //         }
        //         else if (Input.GetMouseButtonDown(1) && inventory.movingSlot.item.CanRightClick(this, tilePosition)) // right click
        //         {
        //             targetPosition = _mainCamera.ScreenToWorldPoint(mousePos);
        //             targetPosition.z = 1; // calculate target pos
        //             targetTilePosition = tilePosition; // already calculated tile position
        //             hasRightClicked = true; // start right click methods
        //         }
        //
        //         ChangeCurrentSlotScale(false, null);
        //     }
        //     else if (!inventory.IsOverSlot())
        //     {
        //         _hoveringText.gameObject.SetActive(true);
        //         if (IsPointerOverComponent<AttackableComponent>())
        //         {
        //             if (_hit.gameObject.GetComponent<AttackableComponent>()
        //                 .DoCanAttackCheck(inventory)) // if we can mine the object
        //             {
        //                 _autoSizeHovering.UpdateText(_hit.gameObject.GetComponent<AttackableComponent>().onHoverText);
        //                 if (Input.GetMouseButtonDown(0) && !_isAttacking)
        //                     SetAttackTarget(_hit.gameObject.gameObject); // set attack target and ready to attack
        //             }
        //         }
        //         else if (IsPointerOverComponent<DroppedItem>())
        //         {
        //             _hoveringText.gameObject.SetActive(true);
        //             _autoSizeHovering.UpdateText("Pick up " + _hit.gameObject.GetComponent<DroppedItem>().item.itemName +
        //                                     " x" +
        //                                     _hit.gameObject.GetComponent<DroppedItem>().count);
        //             if (Input.GetMouseButtonDown(0) &&
        //                 !_isAttacking) // If the player clicks while hovering over a dropped item
        //             {
        //                 SetAttackTarget(_hit.gameObject.gameObject);
        //             }
        //         }
        //         else if (IsPointerOverComponent<InteractableComponent>()) // Checking for Interactable component
        //         {
        //             InteractableComponent interactableComponent = _hit.gameObject.GetComponent<InteractableComponent>();
        //             _hoveringText.gameObject.SetActive(true);
        //             _autoSizeHovering.UpdateText("Interact");
        //
        //             if (Input.GetMouseButtonDown(0) && !_isAttacking)
        //             {
        //                 SetAttackTarget(_hit.gameObject
        //                     .gameObject); // Assuming you're using the same mechanism to move to the object as with the attack target
        //                 interactableComponent
        //                     .Interact(gameObject); // Execute the action from the Interactable component
        //             }
        //         }
        //         else
        //         {
        //             canMoveToMouse = true;
        //             _autoSizeHovering.UpdateText("Walk");
        //             if (!_isMovingToTarget && !_isAttacking) ResetAttackTargetAndMoving();
        //         }
        //         
        //         _isOverSlot = false;
        //         ChangeCurrentSlotScale(false, null);
        //     }
        //     else
        //     {
        //         _hoveringText.gameObject.SetActive(false);
        //     }
        // }
        // else
        // {
        //     canMoveToMouse = false;
        //     _hoveringText.gameObject.SetActive(false);
        // }
        //
        //
        // #endregion
    }

    void HandleLocalPlayer()
    {
        if(_mainCamera == null) {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            if(_mainCamera == null) {
                Debug.LogWarning("Main camera not found");
            }
        }
        
        if(world == null) {
            world = WorldGenerator.Instance;
            if(world == null) {
                Debug.LogWarning("WorldGenerator instance not found");
            }
        }

        if (tileIndicator == null)
        {
            tileIndicator = TileIndicatorSingleton.Instance;
            if (tileIndicator == null) Debug.LogWarning("_tileIndicator not found");
        }

        if (_hoveringText == null)
        {
            _hoveringText = GameObject.FindGameObjectWithTag("HoveringText").GetComponent<TMP_Text>();
            if (_hoveringText == null) Debug.LogWarning("_hoveringText not found");
        }
        
        if (_autoSizeHovering == null)
        {
            _autoSizeHovering = _hoveringText.GetComponent<AutoSizeTMP>();
            if (_autoSizeHovering == null) Debug.LogWarning("_autoSizeHovering not found");
        }
        
        if (!KeyBindingManager.Instance.isWaitingForKeyPress && 
            Input.GetKeyUp(KeyBindingManager.Instance.bindings.OpenConsole))
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
        
        if (Time.timeScale == 0f) // Game is frozen
        {
            return; // Early exit from the Update method
        }
        
        mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(_mainCamera.transform.position.z);
        Vector3 mouseWorldPoint = _mainCamera.ScreenToWorldPoint(mousePos);
        
        // Ensure the z-coordinate is set appropriately
        mouseWorldPoint.z = 1;
        
        // Round to the nearest whole number to get the tile position
        Vector3Int intPosition = world.ClampVector3(mouseWorldPoint);
        
        Vector3Int tilePosition = world.triggerTilemap.WorldToCell(intPosition);
        tileIndicator.transform.position = tilePosition;
        
        if (inventory.movingSlot.item != null && inventory.movingSlot.item.GetTilePlacer() != null)
        {
            tileIndicator.SetActive(true);
        }
        else
        {
            tileIndicator.SetActive(false);
        }
        
        horizontal = 0f;
        vertical = 0f;
        
        //  basic movement ------------------------
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
        
        HealthChanges();
        AdjustSortingLayers();
        UpdateAnimations();
        
        // attacking & mining ---------------------------------------
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
        
        // clicking -------------------------------------------------------------
        if (Input.GetMouseButton(0)) // if we left clicked and no target - move to mouse
        {
            if (attackTarget != null) _isMovingToTarget = true;
            else if (canMoveToMouse && !_isMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
            isMovingToMouse = false;
        
        
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

        if (hasRightClicked)
        {
            MoveToTileEdge();
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

                rb.velocity = movement * moveSpeed; 
            }
        }
        else
        {
            ResetAttackTargetAndMoving();
            rb.velocity = Vector2.zero; // if hit - stop
        } 
        
        if (isInCar && car != null) // in car
        {
            CarLogic();
        }
        
        
        #region Hovering

        _hoveringText.transform.position = Input.mousePosition + AdjustOffsetForScreenBorders(hoveringOffset, invertedHoveringOffset);

        if (!EventSystem.current.IsPointerOverGameObject()) // if not hovering over the button (can be annoying)
        {
            if (inventory.FindClosestSlotItem() != null && inventory.FindClosestSlotItem().item != null)
            {
                canMoveToMouse = false;
                _hoveringText.gameObject.SetActive(true);
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
                _hoveringText.gameObject.SetActive(true);
                _autoSizeHovering.UpdateText("");
                
                _isOverSlot = true;
                ChangeCurrentSlotScale(true, new Vector3(1.15f, 1.15f, 1.15f));
            }
            else if (inventory.isMovingItem && !inventory.IsOverSlot())
            {
                _hoveringText.gameObject.SetActive(true);
                _autoSizeHovering.UpdateText("Drop");
                if (Input.GetMouseButtonDown(0)) // if left clicked
                {
                    Vector3 screenPos = Input.mousePosition;
                    screenPos.z = Mathf.Abs(_mainCamera.transform.position.z);
                    targetPosition = _mainCamera.ScreenToWorldPoint(screenPos);

                    targetPosition.z = 1;
                    isDropping = true;
                }
                else if (Input.GetMouseButtonDown(1) && inventory.movingSlot.item.CanRightClick(this, tilePosition)) // right click
                {
                    targetPosition = _mainCamera.ScreenToWorldPoint(mousePos);
                    targetPosition.z = 1; // calculate target pos
                    targetTilePosition = tilePosition; // already calculated tile position
                    hasRightClicked = true; // start right click methods
                }

                ChangeCurrentSlotScale(false, null);
            }
            else if (!inventory.IsOverSlot())
            {
                _hoveringText.gameObject.SetActive(true);
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
                    _hoveringText.gameObject.SetActive(true);
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
                    _hoveringText.gameObject.SetActive(true);
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
                _hoveringText.gameObject.SetActive(false);
            }
        }
        else
        {
            canMoveToMouse = false;
            _hoveringText.gameObject.SetActive(false);
        }


        #endregion
    }
    
    private void Update()
    {
        if (isLocalPlayer)
        {
            HandleLocalPlayer();
        }
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
        RectTransform textRectTransform = _hoveringText.GetComponent<RectTransform>();

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
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPosition);

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
        screenPos.z = Mathf.Abs(_mainCamera.transform.position.z);
        mousePos = _mainCamera.ScreenToWorldPoint(screenPos);

        // Reset the Z coordinate to match your 2D world
        mousePos.z = 1;
        
        float distance = Vector2.Distance(mousePos, rb.position);
        
        // calculating direction
        direction = ((Vector2)mousePos - rb.position).normalized;
        horizontal = direction.x;
        vertical = direction.y; // setting up these variables for animations to work
        
        // moving
        if (distance > bufferCursorDistance) // if cursor is far away
        {
            rb.velocity = direction * moveSpeed; // simply move
        }
        else if (distance > minCursorDistance) // if cursor is in buffer distance
        {
            // Interpolate velocity from full to zero within the buffer zone
            float bufferFraction = (distance - minCursorDistance) / (bufferCursorDistance - minCursorDistance);
            rb.velocity = direction * (moveSpeed * bufferFraction);
        }
        else // else (if cursor is in minimum distance) dont move
        {
            rb.velocity = Vector2.zero;
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