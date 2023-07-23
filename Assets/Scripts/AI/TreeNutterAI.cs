using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class TreeNutterAI : MonoBehaviour
{
    [Header("General")] 
    public float viewDistance = 10f;
    public Transform player;

    [Header("Movement")]
    public float speed = 3f;
    public float newTargetFreq = 2f;
    public Vector2 movement;

    [Header("Attack")] 
    public float attackCooldown = 3f;
    public int damage = 5;
    public float attackDistance = 0.5f;
    public bool isAttacking;
    public float _dotProductForward;
    public float _dotProductRight;
    private float m_LastAttackTime;

    [Header("Animations")] 
    private Animator m_Animator;
    public bool animLocked;
    public string attackX;
    public string attackY;
    public float attackAnimationDuration = 75f;
    public string movementX;
    public string movementY;
    
    [Header("World")]
    // public WorldGenerator worldGenerator;
    public Vector2 worldBounds;

    private Vector3 m_TargetPosition;
    private float m_TimeSinceLastTarget;
    private Transform m_Transform;
    private Rigidbody2D m_Rb;
    
    private enum State
    {
        Wandering,
        Chasing,
        Attacking
    }
    private State m_CurrentState;

    public void Start()
    {
        m_Transform = transform;
        m_Rb = GetComponent<Rigidbody2D>();
        m_Animator = GetComponent<Animator>();
    }
    
    private void UpdateAnimations()
    {
        if (!animLocked)
        {
            if (movement != Vector2.zero && m_CurrentState != State.Attacking) // if we are moving and not attacking
            {
                m_Animator.SetFloat(movementX, Mathf.Sign(movement.x));
                m_Animator.SetFloat(movementY, Mathf.Sign(movement.y));
                m_Animator.Play("Walk");
            }
            else if (m_CurrentState == State.Attacking)
            {
                m_Animator.SetFloat(attackX, Mathf.Sign(_dotProductRight));
                m_Animator.SetFloat(attackY, Mathf.Sign(_dotProductForward));
                m_Animator.Play("Attack");
            }
            else
            {
                // idle here
            }
        }
    }

    public void Update()
    {
        if (isAttacking)
        {
            Attacking();
            m_Rb.velocity = Vector2.zero;
            return;
        }
        
        switch(m_CurrentState)
        {
            case State.Wandering:
                Wandering();
                break;
            case State.Chasing:
                Chasing();
                break;
            case State.Attacking:
                Attacking();
                break;
        }
        
        MoveToTarget();
        UpdateAnimations();
    }

    private void GetNewTarget()
    {
        // Choose a random position within the world
        float x = Random.Range(-worldBounds.x / 2, worldBounds.x / 2);
        float y = Random.Range(-worldBounds.y / 2, worldBounds.y / 2);
        m_TargetPosition = new Vector3(x, y, transform.position.z);
    }

    private void MoveToTarget()
    {
        if (isAttacking)
        {
            movement = new Vector2(0, 0); // 0 movement so enemy doesn't move
            return;
        }
        
        Vector3 directionToTarget = (m_TargetPosition - m_Transform.position).normalized;

        float horizontal = directionToTarget.x;
        float vertical = directionToTarget.y;

        // Normalize the inputs
        horizontal = Mathf.Clamp(horizontal, -1, 1);
        vertical = Mathf.Clamp(vertical, -1, 1);
    
        movement = new Vector2(horizontal, vertical) * speed * Time.deltaTime;
        Vector2 nextPosition = m_Transform.position + directionToTarget * speed * Time.deltaTime;
        m_Rb.MovePosition(nextPosition);
    }
    
    private void Wandering()
    {
        if (isAttacking) return;
        
        m_TimeSinceLastTarget += Time.deltaTime;

        if (m_TimeSinceLastTarget >= newTargetFreq)
        {
            GetNewTarget();
            m_TimeSinceLastTarget = 0f;
        }

        MoveToTarget();

        // Find the closest player
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(m_Transform.position, viewDistance);
        float closestDistance = float.MaxValue;
        Transform closestPlayer = null;

        foreach(Collider2D nearbyObject in nearbyObjects)
        {
            if(nearbyObject.CompareTag("Player"))
            {
                float distance = Vector2.Distance(m_Transform.position, nearbyObject.transform.position);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = nearbyObject.transform;
                }
            }
        }

        // If there is a player close enough, start chasing them
        if(closestPlayer != null)
        {
            player = closestPlayer;
            m_CurrentState = State.Chasing;
        }
    }
    
    private void Chasing()
    {
        if(isAttacking) return;
        
        // Make sure there is still a player to chase
        if(player == null)
        {
            m_CurrentState = State.Wandering;
            return;
        }

        if (player != null)
        {
            // Change the target position to the player's position
            m_TargetPosition = player.position;
            MoveToTarget();
        
            // If the player is too far, stop chasing them
            if(Vector2.Distance(m_Transform.position, player.position) > viewDistance)
            {
                m_CurrentState = State.Wandering;
                player = null; // Forget about the player
            }
        
            if(Vector2.Distance(m_Transform.position, player.position) <= attackDistance)
            {
                m_CurrentState = State.Attacking;
            }
        }
    }
    
    IEnumerator EndAttack(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }
    
    public void DealDamage()
    {
        if(player != null && Vector2.Distance(m_Transform.position, player.position) <= attackDistance) // if we have a target player and player is still in range
        {
            AttackableComponent health = player.GetComponent<AttackableComponent>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }


    private void Attacking()
    {
        // If the player is too far away or null, stop attacking
        if (player == null || Vector2.Distance(m_Transform.position, player.position) > attackDistance)
        {
            m_CurrentState = State.Wandering;
            return;
        }

        // If it's not yet time for the next attack, do nothing
        if (Time.time < m_LastAttackTime + attackCooldown)
        {
            return;
        }
        
        // deal the damage in animation

        if (player != null)
        {
            Transform rbTransform = m_Rb.transform;
            Vector3 toObjectVector = (player.transform.position - rbTransform.position).normalized;
    
            // Since it's a 2D top-down view game, your forward vector will be along the Y axis
            // Right vector will be along the X axis
            Vector3 playerForward = rbTransform.up;
            Vector3 playerRight = rbTransform.right;
            
            _dotProductForward = Vector3.Dot(toObjectVector, playerForward);
            _dotProductRight = Vector3.Dot(toObjectVector, playerRight);
        }
        
        
        // Start the cooldown
        m_LastAttackTime = Time.time;
        // Set isAttacking to true
        isAttacking = true;
        
        // Start the coroutine to end the attack
        StartCoroutine(EndAttack(attackAnimationDuration));
    }
}
