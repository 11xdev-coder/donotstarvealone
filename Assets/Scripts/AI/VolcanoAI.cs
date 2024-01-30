using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class VolcanoAI : MonoBehaviour
{
    private WorldGenerator _world;
    private CameraFollow _camera;
    private PlayerController _player;
    private Dynamic2DObject _dynamic2DObject;
    
    [Header("Particles")]
    public ParticleSystem volcanoFire;
    public ParticleSystem volcanoSpark;
    public ParticleSystem volcanoSmoke;

    [Header("Burst")] 
    public bool isBursting;
    public int burstDuration;
    public int damageDuration;
    public int preDamageCooldown;
    public int burstCooldown;

    [Header("Shaking")] 
    public float maxDistance;
    public float shakeMultiplier;

    [Header("Damage")] 
    public int damage;
    public float radius;
    
    void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        _camera = FindFirstObjectByType<CameraFollow>();
        _world = FindFirstObjectByType<WorldGenerator>();
        _dynamic2DObject = GetComponent<Dynamic2DObject>();
    }

    IEnumerator Burst()
    {
        isBursting = true;
        volcanoFire.Play();
        volcanoSpark.Play();
        volcanoSmoke.Play();

        yield return new WaitForSeconds(burstDuration);

        isBursting = false;
    }
    
    IEnumerator Damage()
    {
        yield return new WaitForSeconds(preDamageCooldown);
        float burstEndTime = _world.globalTicks + damageDuration;

        while (_world.globalTicks < burstEndTime)
        {
            ApplyDamageInRadius();

            yield return new WaitForSeconds(1f);
        }
    }
    
    IEnumerator TriggerShakes()
    {
        float endTime = _world.globalTicks + burstDuration;
        
        while (_world.globalTicks < endTime)
        {
            TriggerShake();
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!isBursting && _dynamic2DObject.IsInView())
        {
            if (_world.globalTicks % burstCooldown <= 0.2f)
            {
                StartCoroutine(Burst());
                StartCoroutine(Damage());
                StartCoroutine(TriggerShakes());
                
                //if(!_camera.isShaking)
                
            }
        }
        else if (!_dynamic2DObject.IsInView())
        {
            volcanoFire.Stop();
            volcanoSpark.Stop();
            volcanoSmoke.Stop();
        }
    }
    
    void ApplyDamageInRadius()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hitCollider in hitColliders)
        {
            AttackableComponent attackable = hitCollider.GetComponent<AttackableComponent>();
            if (attackable != null && !attackable.isMineable)
            {
                attackable.DealDamage(damage);
            }
        }
    }
    
    private void TriggerShake()
    {
        if (_camera == null || _player == null)
            return;

        // distance to player
        float distanceToPlayer = Vector3.Distance(_player.transform.position, transform.position);

        // calculate the intensity
        float shakeIntensity = Mathf.Clamp01(1 - (distanceToPlayer / maxDistance));
    
        // player is too far
        if (shakeIntensity <= 0)
            return;
    
        // adjust the magnitude
        float shakeMagnitude = shakeIntensity * shakeMultiplier;

        // start a short shake
        StartCoroutine(_camera.Shake(0.1f, shakeMagnitude));
    }

}
