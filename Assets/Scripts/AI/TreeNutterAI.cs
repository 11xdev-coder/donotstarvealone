using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TreeNutterAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float newTargetFreq = 2f;
    public Vector2 movement;

    [Header("Animations")] 
    private Animator m_Animator;
    public bool animLocked;
    
    [Header("World")]
    public WorldGenerator worldGenerator;
    public Vector2 worldBounds;

    private Vector3 m_TargetPosition;
    private float m_TimeSinceLastTarget;
    private Transform m_Transform;
    private Rigidbody2D m_Rb;

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
            if (movement != Vector2.zero)
            {
                m_Animator.SetFloat("Movement X", Mathf.Sign(movement.x));
                m_Animator.SetFloat("Movement Y", Mathf.Sign(movement.y));
                m_Animator.Play("Walk");
            }
            else
            {
                
            }
        }
    }

    public void Update()
    {
        m_TimeSinceLastTarget += Time.deltaTime;

        if (m_TimeSinceLastTarget >= newTargetFreq)
        {
            GetNewTarget();
            m_TimeSinceLastTarget = 0f;
        }

        MoveToTarget();
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
        Vector3 directionToTarget = (m_TargetPosition - m_Transform.position).normalized;

        float horizontal = directionToTarget.x;
        float vertical = directionToTarget.y;

        // Normalize the inputs
        horizontal = Mathf.Clamp(horizontal, -1, 1);
        vertical = Mathf.Clamp(vertical, -1, 1);
    
        movement = new Vector2(horizontal, vertical) * speed * Time.deltaTime;
        Vector2 nextPosition = m_Transform.position + directionToTarget * speed * Time.deltaTime;
        m_Rb.MovePosition(nextPosition);
        
        
        
        UpdateAnimations();
    }
}
