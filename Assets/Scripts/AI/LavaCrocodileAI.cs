using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class LavaCrocodileAI : MonoBehaviour
{
    [Header("Movement")] 
    public float speed;
    [Tooltip("Maximum radius of new targets when wandering")]
    public int wanderTargetRange;
    [Tooltip("Cooldown between getting new targets for wandering")]
    public int wanderTargetCooldown;
    private float _cooldownEnd;
    private float _dotProductRight;
    private float _dotProductForward;
    private Vector3 _movement;
    private Rigidbody2D _rb;
    private Vector3 m_TargetPosition;
    private bool _isMovingToTarget;
    private Vector3 _direction;
    private WorldGenerator _world;

    [Header("Attacking")] 
    public int biteDamage;
    public int scratchDamage;
    public bool isAttacking;
    public int attackRadius;
    public float attackCooldown = 2f; 
    
    public int playerDetectionRadius;
    public float chaseCheckCooldown;
    private float _nextChaseCheckTime;
    private float _nextAttackTime;
    private float _lastAttackTime;
    private enum AttackType { Bite, Scratch }
    private AttackType _selectedAttack;
    
    [Header("Animations")] 
    public string movementX;
    public string movementY;
    public string attackX;
    public string attackY;
    public bool animLocked;
    private Animator m_Animator;

    private AttackableComponent m_Health;

    public enum State
    {
        Wandering,
        Chasing,
        Attacking
    }
    public State currentState;
    
    private void UpdateAnimations()
    {
        if (!animLocked)
        {
            if (_rb.velocity != Vector2.zero && currentState != State.Attacking)
            {
                m_Animator.SetFloat(movementX, Mathf.Sign(_movement.x));
                m_Animator.SetFloat(movementY, Mathf.Sign(_movement.y));
                m_Animator.Play("walk");
            }
            else if (isAttacking || currentState == State.Attacking)
            {
                m_Animator.SetFloat(attackX, Mathf.Sign(_dotProductRight));
                m_Animator.SetFloat(attackY, Mathf.Sign(_dotProductForward));
            }
            else
            {
                m_Animator.Play("idle");
            }
        }
    }

    public void DisableAnimLock()
    {
        animLocked = false;
    }
    
    private void AddListeners()
    {
        if (m_Health.onDeath != null) m_Health.onDeath.AddListener(HandleDeath);
        if (m_Health.onDamageTaken != null) m_Health.onDamageTaken.AddListener(HandleDamage);
    }
    
    private void RemoveListeners()
    {
        if (m_Health.onDeath != null) m_Health.onDeath.RemoveListener(HandleDeath);
        if (m_Health.onDamageTaken != null) m_Health.onDamageTaken.RemoveListener(HandleDamage);
    }
    
    private void HandleDeath()
    {
        RemoveListeners();
    }

    private void HandleDamage()
    {
        
    }
    
    // Start is called before the first frame update
    void Start()
    {
        m_Animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        m_Health = GetComponent<AttackableComponent>();
        _world = FindFirstObjectByType<WorldGenerator>();
        
        AddListeners();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAttacking && currentState != State.Attacking)
        {
            switch (currentState)
            {
                case State.Wandering:
                    if (_world.globalTicks > _cooldownEnd)
                    {
                        Wander();
                    }
                    break;
                case State.Chasing:
                    Chase();
                    break;
            }

            if (_isMovingToTarget)
            {
                MoveToCurrentTarget();
            }
            if (_world.globalTicks >= _nextChaseCheckTime)
            {
                _nextChaseCheckTime = _world.globalTicks + chaseCheckCooldown;
                CheckForPlayer(playerDetectionRadius);
            }
        }
        
        if (ShouldStartAttack())
        {
            _rb.velocity = Vector2.zero;
            if (_world.globalTicks >= _nextAttackTime)
            {
                Attack();
            }
        }
        
        if (_movement.sqrMagnitude > 1)
        {
            _movement.Normalize();
        }

        _rb.velocity = _movement * speed; 
        UpdateAnimations();
    }

    private Vector3 GetNewWanderTarget()
    {
        int x = Random.Range(Mathf.RoundToInt(_rb.position.x) - wanderTargetRange, Mathf.RoundToInt(_rb.position.x) + wanderTargetRange);
        int y = Random.Range(Mathf.RoundToInt(_rb.position.y) - wanderTargetRange, Mathf.RoundToInt(_rb.position.y) + wanderTargetRange);
        
        if (!_world.collidableTilemap.HasTile(new Vector3Int(x, y))) // not an ocean tile
        {
            return new Vector3(x, y);
        }

        return GetNewWanderTarget();
    }
    
    private void Wander()
    {
        _cooldownEnd = _world.globalTicks + wanderTargetCooldown;
        m_TargetPosition = GetNewWanderTarget();
        currentState = State.Wandering;
        _isMovingToTarget = true;
    }
    
    private GameObject CheckForPlayer(int radius)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Player"))
            {
                currentState = State.Chasing;
                return hitCollider.gameObject;
            }
        }

        // no player is found
        if (currentState == State.Chasing)
        {
            currentState = State.Wandering;
        }

        return null;
    }
    
    private void Chase()
    {
        GameObject player = CheckForPlayer(playerDetectionRadius);
        if (player != null)
        {
            m_TargetPosition = player.transform.position;
            _isMovingToTarget = true;
            currentState = State.Chasing;
        }
    }

    private void Attack()
    {
        if (!isAttacking && _world.globalTicks >= _nextAttackTime)
        {
            GameObject player = CheckForPlayer(attackRadius);
            if (player != null)
            {
                _selectedAttack = (AttackType)Random.Range(0, 2); // choose between bite (0) and scratch (1)
                // Animation and attack logic
                StartAttackAnimation();
                _nextAttackTime = _world.globalTicks + attackCooldown;
                isAttacking = true;
            }
            else
            {
                isAttacking = false;
                currentState = State.Chasing;
            }
        }
        else
        {
            isAttacking = false;
            currentState = State.Chasing;
        }
    }

    private void StartAttackAnimation()
    {
        switch (_selectedAttack)
        {
            case AttackType.Bite:
                m_Animator.Play("bite");
                break;
            case AttackType.Scratch:
                m_Animator.Play("scratch");
                break;
        }
    }

    private void MoveToCurrentTarget()
    {
        var position = _rb.position;
        _direction = (m_TargetPosition - (Vector3) position).normalized;
        
        Vector3 directionDifference = m_TargetPosition - (Vector3)position;
        if (directionDifference.sqrMagnitude < 0.1 * 0.1)
        {
            _movement = Vector3.zero;

            _isMovingToTarget = false;
        }
        else
        {
            _direction = directionDifference.normalized;
            _movement = new Vector3(_direction.x, _direction.y);
        }
    }
    
    // Called via animation event
    public void FinishAttack()
    {
        isAttacking = false;
        currentState = State.Wandering; // Or Chasing if the player is still close
        // Ensure crocodile's scale is correct and visible
        transform.localScale = Vector3.one; 
    }

    // Called via animation event at the moment of attack impact
    public void DealDamageToPlayer()
    {
        GameObject player = CheckForPlayer(attackRadius);
        if (player != null)
        {
            int damage = _selectedAttack == AttackType.Bite ? biteDamage : scratchDamage;
            player.GetComponent<AttackableComponent>().DealDamage(damage);
        }
    }   
    
    private bool ShouldStartAttack()
    {
        GameObject player = CheckForPlayer(attackRadius);
        if (player != null) return true;
        return false;
    }
}
