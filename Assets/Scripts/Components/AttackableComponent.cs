using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class AttackableComponent : MonoBehaviour
{
    public int health;
    public int maxHealth;
    public GameObject self;
    
    [Header("Events")]
    [CanBeNull] public UnityEvent onDamageTaken;
    [CanBeNull] public UnityEvent onDeath;
    
    [Header("Drops")]
    public bool doDrops;
    public List<ItemClass> drops;
    
    [Header("Enemy spawn")]
    public bool doEnemySpawn;
    public GameObject[] enemiesToSpawn;
    
    [Header("Particles")]
    public bool doDeathParticles;
    public ParticleSystem deathParticles;
    
    [Header("Sounds")]
    public bool hasHitSound;
    public AudioClip hitSound;

    [Header("Mining")] 
    public int pickaxePower;
    public int axePower;
    public bool isMineable;
    public Vector3 attackOffset;
    
    [Header("Other")]
    public string onHoverText;
    public bool changeSpriteDependingOnHealth;
    public bool destroyOnDeath;
    public Sprite[] damagedSprites;
    
    private AudioSource m_AudioSource;
    private SpriteRenderer m_SpriteRenderer;

    public void Start()
    {
        self = gameObject;
        if (doDeathParticles) deathParticles = deathParticles.GetComponent<ParticleSystem>();
        if(hasHitSound) m_AudioSource = GetComponent<AudioSource>();
        
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSpriteBasedOnHealth();
    }
    
    public void Update()
    {
        health = Math.Clamp(health, 0, maxHealth);
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;
        onDamageTaken?.Invoke(); // invoke the event after taking damage because some scripts depend on it
        print($"{gameObject.name} took {dmg} damage");
        
        UpdateSpriteBasedOnHealth();
        if (hasHitSound)
        {
            m_AudioSource.PlayOneShot(hitSound);
        }
    
        if (health <= 0)
        {
            Dead();
        }
    }

    private void Dead()
    {
        onDeath?.Invoke();
        
        if (doEnemySpawn)
        {
            foreach (GameObject enemy in enemiesToSpawn)
            {
                Instantiate(enemy, transform.position, Quaternion.identity);
            }
        }

        if (doDrops)
        {
            foreach (ItemClass item in drops)
            {
                SlotClass slot = new SlotClass(item, 1);
                
                item.SpawnItemAsDropped(slot, transform);
            }
        }

        if (doDeathParticles)
        {
            ParticleSystem newDeathParticles = Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y, 5), Quaternion.identity);
            newDeathParticles.Play();
        }
        if(destroyOnDeath) Destroy(gameObject);
    }
    
    public bool DoCanAttackCheck(InventoryManager inventory)
    {
        if (isMineable)
        {
            if (inventory.equippedTool != null && inventory.equippedTool.item != null)
            {
                if (inventory.equippedTool.item.pickaxePower >= pickaxePower &&
                    inventory.equippedTool.item.axePower >= axePower) return true;
            }
        }
        else return true;
            
        
        return false;
    }

    private void UpdateSpriteBasedOnHealth()
    {
        if (changeSpriteDependingOnHealth)
        {
            // This formula will calculate sprite depending on health
            // Sprites will be calculated from the end, like first damaged sprites will come from the end
            int spriteIndex = health / 20;
        
            // Clamp the index to be within the array bounds
            spriteIndex = Mathf.Clamp(spriteIndex, 0, damagedSprites.Length - 1);
        
            // Change the sprite
            m_SpriteRenderer.sprite = damagedSprites[spriteIndex];
        }
    }
}
