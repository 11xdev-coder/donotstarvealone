using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{
    // NOT USED ANYMORE LOOK AttackableComponent
    
    // public int health;
    //
    // public int maxHealth;
    // [CanBeNull] public UnityEvent onDamageTaken;
    //
    // [Header("Do's")]
    // public bool doDrops;
    // public bool doEnemySpawn;
    // public bool doDeathParticles;
    //
    // [Header("Assignable")]
    // public GameObject[] enemiesToSpawn;
    // public ParticleSystem deathParticles;
    //
    // public void Awake()
    // {
    //     health = maxHealth;
    // }
    //
    // public void Update()
    // {
    //     health = Math.Clamp(health, 0, maxHealth);
    // }
    //
    // public void TakeDamage(int dmg)
    // { 
    //     onDamageTaken?.Invoke();
    //     
    //     health -= dmg;
    //     print($"{gameObject.name} took {dmg} damage");
    //     
    //     if (health <= 0)
    //     {
    //         Dead();
    //     }
    //
    // }
    //
    // private void Dead()
    // {
    //     if (doEnemySpawn)
    //     {
    //         foreach (GameObject enemy in enemiesToSpawn)
    //         {
    //             Instantiate(enemy, transform.position, Quaternion.identity);
    //         }
    //     }
    //     Destroy(gameObject);
    // }
}
