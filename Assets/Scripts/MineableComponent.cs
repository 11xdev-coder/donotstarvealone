using System;
using UnityEngine;

public class MineableComponent : MonoBehaviour
{
    public int health;
    
    [Header("Bools")]
    public bool doDrops;
    public bool doEnemySpawn;
    public bool doDeathParticles;
    public string onHoverText;
    public bool hasHitSound;

    [Header("Assignable")] 
    public ToolClass tool;
    public GameObject[] enemiesToSpawn;
    public ParticleSystem deathParticles;
    public Sprite[] damagedSprites;
    public AudioClip hitSound;


    private AudioSource _audioSource;
    private SpriteRenderer _spriteRenderer;

    public void Start()
    {
        if (doDeathParticles) deathParticles = deathParticles.GetComponent<ParticleSystem>();
        if(hasHitSound) _audioSource = GetComponent<AudioSource>();
        
        _spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSpriteBasedOnHealth();
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;
        print($"{gameObject.name} took {dmg} damage");
        
        UpdateSpriteBasedOnHealth();
        if (hasHitSound)
        {
            _audioSource.PlayOneShot(hitSound);
        }
        
        if (health <= 0)
        {
            Dead();
        }

    }

    private void Dead()
    {
        if (doEnemySpawn)
        {
            foreach (GameObject enemy in enemiesToSpawn)
            {
                Instantiate(enemy, transform.position, Quaternion.identity);
            }
        }

        if (doDeathParticles)
        {
            ParticleSystem newDeathParticles = Instantiate(deathParticles, new Vector3(transform.position.x, transform.position.y, 5), Quaternion.identity);
            newDeathParticles.Play();
        }
        Destroy(gameObject);
    }

    public bool DoCanMineCheck(InventoryManager inventory)
    {
        if (inventory.equippedTool != null && inventory.equippedTool.item != null)
        {
            if (inventory.equippedTool.item.GetTool() == tool) return true;
        }
        
        return false;
    }

    public void UpdateSpriteBasedOnHealth()
    {
        // This formula will calculate sprite depending on health
        // Sprites will be calculated from the end, like first damaged sprites will come from the end
        int spriteIndex = health / 20;
        
        // Clamp the index to be within the array bounds
        spriteIndex = Mathf.Clamp(spriteIndex, 0, damagedSprites.Length - 1);
        
        // Change the sprite
        _spriteRenderer.sprite = damagedSprites[spriteIndex];
    }
}