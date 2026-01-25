using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    [Serializable]
    public class WeaponSlot
    {
        public WeaponType type;
        public bool isActive;
        public float interval;
        public float timer;
        public int damage;
        public int pelletCount;
        public float spreadAngle;
    }

    public enum WeaponType
    {
        None,
        Rifle,
        Shotgun
    }

    public List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Header("Player Stats")]
    public float moveSpeed = 6f;
    public int maxHp = 10;
    public int strength = 0;
    public float attackSpeedMultiplier = 1f;
    public int pendingUpgradePoints = 0;

    [Header("Movement")]
    Rigidbody2D rb;
    Vector2 inputMove;

    [Header("Health")]
    public int currentHp;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public int maxWeaponSlots = 6;

    [Header("Knockback")]
    public float knockbackStrength = 6f;
    public float knockbackDuration = 0.12f;

    [Header("Invulnerability (i-frames)")]
    public float invulnDuration = 0.35f;
    Vector2 knockbackVel;
    float knockbackTimer;
    float invulnTimer;

    [Header("XP & Leveling")]
    public int playerLevel = 1;
    public int currentXp = 0;
    public int xpToNextLevel = 5;
    public int gold = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHp = maxHp;

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            weaponSlots[i].isActive = (i == 0); // sadece ilk silah aktif
        }

        InitWeaponSlots();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputMove = new Vector2(h, v).normalized;

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            WeaponSlot slot = weaponSlots[i];
            if (!slot.isActive) continue;

            slot.timer -= Time.deltaTime;
            if (slot.timer <= 0f)
            {
                Fire(slot);
                slot.timer = Mathf.Max(0.08f, slot.interval / attackSpeedMultiplier);
            }
        }
    }

    void FixedUpdate()
    {
        // i-frame timer
        if (invulnTimer > 0f)
            invulnTimer -= Time.fixedDeltaTime;

        // knockback timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
                knockbackVel = Vector2.zero;
        }

        Vector2 finalVel = (inputMove * moveSpeed) + knockbackVel;
        rb.MovePosition(rb.position + finalVel * Time.fixedDeltaTime);
    }

    public bool TrySpendGold(int cost)
    {
        if (gold < cost) { return false; }
        gold -= cost;
        return true;
    }

    public void AddGold(int amount) => gold += amount;

    void InitWeaponSlots()
    {
        weaponSlots.Clear();

        for (int i = 0; i < maxWeaponSlots; i++)
        {
            weaponSlots.Add(new WeaponSlot
            {
                isActive = false,
                type = WeaponType.None
            });
        }

        weaponSlots[0] = CreateDefaultWeapon(WeaponType.Rifle);
        weaponSlots[0].isActive = true;
    }


    Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        Transform nearest = null;
        float bestDist = float.MaxValue;
        Vector2 myPos = transform.position;

        foreach (var e in enemies)
        {
            float d = Vector2.SqrMagnitude((Vector2)e.transform.position - myPos);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = e.transform;
            }
        }

        return nearest;
    }

    bool TryGetDirectionToNearest(out Vector2 dir)
    {
        dir = Vector2.zero;

        if (firePoint == null) return false;

        Transform target = FindNearestEnemy();
        if (target == null) return false;

        dir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        return true;
    }

    void SpawnBullet(Vector2 dir, int damage)
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null) return;

        bullet.damage = damage + strength;
        bullet.SetDirection(dir);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("XP"))
        {
            currentXp += 1;
            Destroy(other.gameObject);
            TryLevelUp();
            return;
        }

        if (!other.CompareTag("Enemy")) return;

        // i-frame aktifse görmezden gel
        if (invulnTimer > 0f) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        // Hasar
        TakeDamage(enemy.contactDamage);

        // Knockback (enemy'den uzak yöne)
        Vector2 dir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        knockbackVel = dir * knockbackStrength;
        knockbackTimer = knockbackDuration;

        // i-frame başlat
        invulnTimer = invulnDuration;
    }

    private void TryLevelUp()
    {
        while (currentXp >= xpToNextLevel)
        {
            playerLevel += 1;
            currentXp -= xpToNextLevel;
            xpToNextLevel += 5; // sonraki seviye için gereken XP artışı
            pendingUpgradePoints += 1;
        }
    }

    void TakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
    }

    void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Fire(WeaponSlot slot)
    {
        switch (slot.type)
        {
            case WeaponType.Rifle:
                FireRifle(slot);
                break;
            case WeaponType.Shotgun:
                FireShotgun(slot);
                break;
            case WeaponType.None:
            default:
                break;
        }
    }


    void FireRifle(WeaponSlot slot)
    {
        if (!TryGetDirectionToNearest(out Vector2 dir)) return;
        SpawnBullet(dir, slot.damage);
    }

    void FireShotgun(WeaponSlot slot)
    {
        if (!TryGetDirectionToNearest(out Vector2 dir)) return;

        int pellets = Mathf.Max(1, slot.pelletCount);

        // pelletCount 1 ise tek mermi gibi çalışsın
        if (pellets == 1)
        {
            SpawnBullet(dir, slot.damage);
            return;
        }

        float startAngle = -slot.spreadAngle / 2f;
        float step = slot.spreadAngle / (pellets - 1);

        for (int i = 0; i < pellets; i++)
        {
            float angle = startAngle + step * i;
            Vector2 rotatedDir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)dir);

            SpawnBullet(rotatedDir, slot.damage);
        }
    }

    public void ChangeStrength(int delta) => strength += delta;

    public void ChangeMoveSpeed(float delta)
    {
        moveSpeed = Mathf.Max(0.5f, moveSpeed + delta);
    }

    public void ChangeAttackSpeedPercent(int deltaPercent)
    {
        // attackSpeedMultiplier 1.0 = %100
        // +10% => +0.10
        float delta = deltaPercent / 100f;
        attackSpeedMultiplier = Mathf.Clamp(attackSpeedMultiplier + delta, 0.2f, 5f);
    }


    public void ChangeMaxHealth(int delta)
    {
        maxHp = Mathf.Max(1, maxHp + delta);
        currentHp = Mathf.Clamp(currentHp + delta, 0, maxHp);
    }

    public bool TryAddWeapon(WeaponType type)
    {
        // weaponSlots listesini maxWeaponSlots kadar baştan doldurmuş olmak en sağlıklısı.
        // Eğer doldurmadıysak, bu fonksiyon ekleyerek de büyütebilir ama ben sabit varsayıyorum.

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (!weaponSlots[i].isActive)
            {
                weaponSlots[i] = CreateDefaultWeapon(type);
                weaponSlots[i].isActive = true;
                return true;
            }
        }

        return false;
    }

    private WeaponSlot CreateDefaultWeapon(WeaponType type)
    {
        WeaponSlot slot = new WeaponSlot();
        slot.type = type;
        slot.isActive = true;
        slot.timer = 0f;

        if (type == WeaponType.Rifle)
        {
            slot.interval = 0.5f;
            slot.damage = 1;
            slot.pelletCount = 1;
            slot.spreadAngle = 0f;
        }
        else if (type == WeaponType.Shotgun)
        {
            slot.interval = 0.9f;
            slot.damage = 1;
            slot.pelletCount = 5;
            slot.spreadAngle = 35f;
        }

        return slot;
    }


}
