using System;
using UnityEngine;
using UnityEngine.Serialization;
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
    public Camera mainCamera;
    
    [Header("Animation")]
    public float horizontal;
    public float vertical;
    private bool _diagonal;
    private bool _playsidewaysIdleAnim;
    
    [Header("Attacking thingies")]
    public GameObject attackTarget;

    public InventoryManager inventory;

    private void Awake()
    {
        canMoveToMouse = true;
        _rb = GetComponent<Rigidbody2D>();
    }

    private bool AnimationIsOver(Animator animatorParam, int layer)
    {
        if (animatorParam.GetCurrentAnimatorStateInfo(layer).normalizedTime > 1)
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
                if(!_diagonal) animator.Play("walkfront");
            
            if (horizontal == 0 & vertical > 0)
                if(!_diagonal) animator.Play("walkbackward");
        
            
        
            if ((vertical == 0 & horizontal > 0) || (vertical != 0 & horizontal > 0)) animator.Play("walkright");
            if ((vertical == 0 & horizontal < 0) || (vertical != 0 & horizontal < 0)) animator.Play("walkleft");
        }
    }

    private void Idle()
    {
        if (_playsidewaysIdleAnim) animator.PlayInFixedTime("idlesideways");
        else animator.PlayInFixedTime("idle");
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

            PlayAnimation();
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
                
                Attack(target);
            }
        }
    }
    
    public void SetAttackTarget(GameObject target)
    {
        // if (attackTarget == null)
        // {
            attackTarget = target;
            canMoveToMouse = false;
        // }
    }

    public void ResetAttackTargetAndMoving()
    {
        // reset attack target and set that we can move to mouse
        canMoveToMouse = true;
        attackTarget = null;
        _isMovingToTarget = false;
    }

    public void Attack(GameObject target)
    {
        Transform rbTransform = _rb.transform;
        Vector3 toObjectVector = (target.transform.position - rbTransform.position).normalized;
    
        // Since it's a 2D top-down view game, your forward vector will be along the Y axis
        // Right vector will be along the X axis
        Vector3 playerForward = rbTransform.up;
        Vector3 playerRight = rbTransform.right;

        float dotProductForward = Vector3.Dot(toObjectVector, playerForward);
        float dotProductRight = Vector3.Dot(toObjectVector, playerRight);

        if (dotProductForward > 0) Debug.Log("front");
        else if (dotProductForward < 0) Debug.Log("back");
        else Debug.Log("not front or back");
        

        if (dotProductRight > 0) Debug.Log("right");
         else if (dotProductRight < 0) Debug.Log("left");
        else Debug.Log("not right or left");
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;
        // if w pressed
        if (Input.GetKey(KeyCode.W))
        {
            _playsidewaysIdleAnim = false;
            vertical = 1;
            PlayAnimation();
            
            ResetAttackTargetAndMoving();
        }
        // if a pressed
        if (Input.GetKey(KeyCode.A))
        {
            _playsidewaysIdleAnim = true;
            _diagonal = true;
            horizontal = -1;
            PlayAnimation();
            
            ResetAttackTargetAndMoving();
        }
        // if d pressed
        if (Input.GetKey(KeyCode.D))
        {
            _playsidewaysIdleAnim = true;
            _diagonal = true;
            horizontal = 1;
            PlayAnimation();
            
            ResetAttackTargetAndMoving();
        }
        // if s pressed
        if (Input.GetKey(KeyCode.S))
        {
            _playsidewaysIdleAnim = false;
            vertical = -1;
            PlayAnimation();
            
            ResetAttackTargetAndMoving();
        }
        
        // if we want to move to target
        if (_isMovingToTarget)
        {
            MoveTowardsTarget(attackTarget); // move
        }
        
        // if left clicked pressed and we have an attacktarget
        if (Input.GetMouseButtonDown(0) && attackTarget != null)
        {
            _isMovingToTarget = true;
        }
        else if (Input.GetMouseButton(0))
        {
            _moveToMouse = true;
            if(canMoveToMouse && !_isMovingToTarget) MoveToMouse();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _moveToMouse = false;
        }
        else
        {
            // idle check
            if (horizontal == 0 & vertical == 0)
            {
                Idle();
            }
            // is s or d pressed
            if (Input.GetKeyUp(KeyCode.A) | Input.GetKeyUp(KeyCode.D))
            {
                _diagonal = false;
            }
        }

        // // if we can move to mouse and we are clicking then move
        // if (_moveToMouse && _canMoveToMouse)
        // {
        //     MoveToMouse();
        // }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            // use selected item
            if(inventory.equippedTool != null)
                inventory.equippedTool.item.Use(this);
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
        PlayAnimation();
    }
}