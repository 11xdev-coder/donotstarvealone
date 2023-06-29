using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public int Health;
    
    public int maxHealth;

    public void Awake()
    {
        Health = maxHealth;
    }

    public void Update()
    {
        Health = Math.Clamp(Health, 0, maxHealth);
    }

    public void TakeDamage(int dmg)
    {
        Health -= dmg;
        print($"{gameObject.name} took {dmg} damage");
        
        if (Health <= 0)
        {
            Dead();
        }

    }

    private void Dead()
    {
        Destroy(gameObject);
    }
}
