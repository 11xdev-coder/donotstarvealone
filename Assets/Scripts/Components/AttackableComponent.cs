using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class AttackableComponent : NetworkBehaviour
{
    [SyncVar] public int health;
    [SyncVar] public int maxHealth;
    public GameObject self;
    
    [Header("Events")]
    [CanBeNull] public UnityEvent onDamageTaken;
    [CanBeNull] public UnityEvent onDeath;
    
    [Header("Drops")]
    [SyncVar] public bool doDrops;
    public List<ItemClass> drops;
    
    [Header("Enemy spawn")]
    [SyncVar] public bool doEnemySpawn;
    public List<GameObject> enemiesToSpawn;
    
    [Header("Particles")]
    [SyncVar] public bool doDeathParticles;
    public ParticleSystem deathParticles;
    
    [Header("Sounds")]
    [SyncVar] public bool hasHitSound;
    public AudioClip hitSound;

    [Header("Mining")] 
    [SyncVar] public int pickaxePower;
    [SyncVar] public int axePower;
    [SyncVar] public bool isMineable;
    public Vector3 attackOffset;
    
    [Header("Other")]
    public string onHoverText;
    public bool changeSpriteDependingOnHealth;
    public bool destroyOnDeath;
    public Sprite[] damagedSprites;
    
    private AudioSource m_AudioSource;
    private SpriteRenderer m_SpriteRenderer;
    private HealthBarComponent _healthBarComponent;
    
    public void Start()
    {
        self = gameObject;
        if (doDeathParticles) deathParticles = deathParticles.GetComponent<ParticleSystem>();
        if(hasHitSound) m_AudioSource = GetComponent<AudioSource>();
        
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSpriteBasedOnHealth(health);

        _healthBarComponent = GetComponent<HealthBarComponent>();
        if(_healthBarComponent != null) _healthBarComponent.SetMaxHealth(maxHealth);
    }
    
    public void Update()
    {
        health = Math.Clamp(health, 0, maxHealth);
    }
    
    public void DealDamage(int dmg)
    {
        if (!isServer) return;
        
        if (dmg == 0) return;
        health -= dmg;
        onDamageTaken?.Invoke(); // invoke the event after taking damage because some scripts depend on it
        print($"{gameObject.name} took {dmg} damage");
        
        ClientChanges(health);
        
        
    }
    
    [ClientRpc]
    void ClientChanges(int h)
    {
        health = h;
        if (hasHitSound)
        {
            m_AudioSource.PlayOneShot(hitSound);
        }
        
        UpdateSpriteBasedOnHealth(health);
        
        if(_healthBarComponent != null) _healthBarComponent.SetHealth(health);
        
        if (health <= 0)
        {
            Dead();
        }
    }
    
    private void Dead()
    {
        if (!isServer) return;
        
        onDeath?.Invoke();
        
        if (doEnemySpawn)
        {
            foreach (GameObject enemyPrefab in enemiesToSpawn)
            {
                var enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
                NetworkServer.Spawn(enemy);
            }
        }

        if (doDrops)
        {
            foreach (ItemClass item in drops)
            {
                SlotClass slot = new SlotClass(item, 1);
                var droppedItem = item.SpawnItemAsDropped(slot, transform);
                if(droppedItem != null) NetworkServer.Spawn(droppedItem);
            }
        }

        if (doDeathParticles)
        {
            ParticleSystem newDeathParticles = Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y, 5), Quaternion.identity);
            NetworkServer.Spawn(newDeathParticles.gameObject);
            newDeathParticles.Play();
        }
        
        if(_healthBarComponent != null) NetworkServer.Destroy(_healthBarComponent.healthBarInstance);

        if (destroyOnDeath)
        {
            NetworkServer.Destroy(gameObject);
            WorldGenerator.Instance.objects[WorldGenerator.Instance.ClampVector3(transform.position)] = null; // remove the object completely
        }
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

    public int GetDamage(InventoryManager inventory)
    {
        if (inventory.equippedTool != null && inventory.equippedTool.item != null)
        {
            return inventory.equippedTool.item.damage;
        }

        return 0;
    }
    
    private void UpdateSpriteBasedOnHealth(int h)
    {
        if (changeSpriteDependingOnHealth)
        {
            // This formula will calculate sprite depending on health
            // Sprites will be calculated from the end, like first damaged sprites will come from the end
            int spriteIndex = h / 20;
        
            // Clamp the index to be within the array bounds
            spriteIndex = Mathf.Clamp(spriteIndex, 0, damagedSprites.Length - 1);
        
            // Change the sprite
            m_SpriteRenderer.sprite = damagedSprites[spriteIndex];
        }
    }
}
