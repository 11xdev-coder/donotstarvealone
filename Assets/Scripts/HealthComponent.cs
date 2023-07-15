using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public int health;
    
    public int maxHealth;
    
    [Header("Do's")]
    public bool doDrops;
    public bool doEnemySpawn;
    public bool doDeathParticles;
    
    [Header("Assignable")]
    public GameObject[] enemiesToSpawn;
    public ParticleSystem deathParticles;

    public void Awake()
    {
        health = maxHealth;
    }

    public void Update()
    {
        health = Math.Clamp(health, 0, maxHealth);
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;
        print($"{gameObject.name} took {dmg} damage");
        
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
        Destroy(gameObject);
    }
}
