using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{
    public Action<int, int> OnHealthChanged; // current, max

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
        public float meleeRange;     // merkezin player’dan uzaklığı
        public float meleeRadius;    // vurma yarıçapı
        public int maxTargets;       // kaç hedef vurabilsin (0 = sınırsız)
        public float baseRange = 6f;
    }

    [Serializable]
    public struct OwnedItem
    {
        public string title;
        public string desc;
        public Sprite icon;
        public int cost;
        public int rarity; // 0-4 (Common..Legendary)
    }

    public enum WeaponType
    {
        None,
        Rifle,  // ✅ ranged
        Shotgun,    // ✅ ranged
        Sniper,   // ✅ ranged

        Sword,    // ✅ melee
        Spear,    // ✅ melee
        Hammer    // ✅ melee
    }

    public List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Header("Inventory (Read-only)")]
    public List<OwnedItem> ownedItems = new List<OwnedItem>();

    public Animator anim;
    Vector2 lastMoveDir = Vector2.down; // varsayılan aşağı baksın

    [SerializeField] private Color normalColor = Color.white;

    [Header("Player Stats")]
    public float moveSpeed = 6f;
    public int maxHp = 10;
    public int strength = 0;
    public float attackSpeedMultiplier = 1f;
    public int pendingUpgradePoints = 0;

    [Header("Defense / Utility Stats")]
    public int armor = 0;                 // flat hasar azaltma
    public float hpRegenPerSec = 0f;      // saniye başı can
    public float pickupRangeBonus = 0f;   // +metre (XpOrb attractRadius’a eklenecek)

    private float regenAccumulator = 0f;  // regen fractional biriksin

    [Header("Critical Stats")]
    [Range(0f, 100f)]
    public float critChance = 0f;      // %
    public float critMultiplier = 1.75f; // sabit (%75 extra)

    [Header("Movement")]
    Rigidbody2D rb;
    Vector2 inputMove;

    [Header("Health")]
    public int currentHp;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public int maxWeaponSlots = 4;

    [Range(0f, 3f)]
    public float rangeBonus = 0f; // % olarak, 0.25 = +25%

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

    [Header("Melee VFX")]
    [SerializeField] private SpearLineVFX spearStabPrefab;
    [SerializeField] private HammerImpactVFX hammerRingPrefab;

    [Header("RNG")]
    [Range(0f, 100f)]
    public float luck = 0f;

    [Header("Hit Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.08f;


    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        normalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        currentHp = maxHp;
        NotifyHealthUI();


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

        bool isMoving = inputMove.sqrMagnitude > 0.001f;

        anim.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            lastMoveDir = inputMove;          // SON YÖNÜ KAYDET
            anim.SetFloat("moveX", inputMove.x);
            anim.SetFloat("moveY", inputMove.y);
        }
        else
        {
            // idle iken son yönü koru
            anim.SetFloat("moveX", lastMoveDir.x);
            anim.SetFloat("moveY", lastMoveDir.y);
        }

        if (lastMoveDir.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(lastMoveDir.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

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

        TickRegen();
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

    public void ResetVisualState()
    {
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        CancelInvoke();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;   // ana reset
        }
    }


    private void TickRegen()
    {
        if (hpRegenPerSec <= 0f) return;
        if (currentHp <= 0) return;
        if (currentHp >= maxHp) return;

        regenAccumulator += hpRegenPerSec * Time.deltaTime;

        // 0.2 gibi değerlerde de çalışsın diye integer’a çevirip basıyoruz
        int heal = Mathf.FloorToInt(regenAccumulator);
        if (heal <= 0) return;

        regenAccumulator -= heal;
        currentHp = Mathf.Clamp(currentHp + heal, 0, maxHp);
        NotifyHealthUI();
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

    bool TryGetDirectionToNearest(out Vector2 dir)
    {
        dir = Vector2.zero;

        if (firePoint == null) return false;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return false;

        Transform nearest = null;
        float bestDist = float.MaxValue;
        Vector2 myPos = firePoint.position;

        foreach (var e in enemies)
        {
            float d = Vector2.SqrMagnitude((Vector2)e.transform.position - myPos);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = e.transform;
            }
        }

        if (nearest == null) return false;

        dir = ((Vector2)nearest.position - myPos).normalized;
        return true;
    }


    bool TryGetDirectionToNearestInRange(float maxRange, out Vector2 dir)
    {
        dir = Vector2.zero;
        if (firePoint == null) return false;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return false;

        Transform nearest = null;
        float bestSqrDist = float.MaxValue;
        Vector2 myPos = firePoint.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            Vector2 ePos = enemies[i].transform.position;
            float d = (ePos - myPos).sqrMagnitude;
            if (d < bestSqrDist)
            {
                bestSqrDist = d;
                nearest = enemies[i].transform;
            }
        }

        if (nearest == null) return false;

        // ✅ menzil kontrolü
        float maxSqr = maxRange * maxRange;
        if (bestSqrDist > maxSqr) return false;

        dir = ((Vector2)nearest.position - myPos).normalized;
        return true;
    }


    void SpawnBullet(Vector2 dir, int damage, float weaponBaseRange)
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Bullet bullet = b.GetComponent<Bullet>();
        if (bullet == null) return;

        bullet.damage = damage + strength;

        // ✅ menzil = silah baseRange * (1 + player rangeBonus)
        float finalRange = weaponBaseRange * (1f + rangeBonus);

        // Bullet.cs’te ekleyeceğimiz fonksiyon
        bullet.SetMaxRange(finalRange);

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

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(1, amount - armor); // ✅ armor
        currentHp -= finalDamage;
        PlayHitFlash();

        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }
        NotifyHealthUI();
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

            case WeaponType.Sniper:
                FireSniper(slot);
                break;

            case WeaponType.Sword:
            case WeaponType.Spear:
            case WeaponType.Hammer:
                FireMelee(slot);
                break;
        }
    }

    void FireMelee(WeaponSlot slot)
    {
        Vector2 dir;
        if (!TryGetDirectionToNearest(out dir))
            dir = lastMoveDir.sqrMagnitude > 0.01f ? lastMoveDir : Vector2.down;

        dir = dir.normalized;

        float rMult = (1f + rangeBonus);
        float meleeRange = slot.meleeRange * rMult;
        float meleeRadius = slot.meleeRadius * rMult;

        Vector2 center = (Vector2)transform.position + dir * meleeRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeRadius);

        int dealt = 0;
        int limit = slot.maxTargets; // 0 => sınırsız

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;

            Enemy e = hits[i].GetComponentInParent<Enemy>();
            if (e == null) continue;

            e.TakeDamage(slot.damage + strength);

            dealt++;
            if (limit > 0 && dealt >= limit) break;
        }

        // ✅ 2) VFX (vuruyor mu hissi)
        SpawnMeleeVFX(slot, dir, center);
    }

    void SpawnMeleeVFX(WeaponSlot slot, Vector2 dir, Vector2 center)
    {
        switch (slot.type)
        {
            case WeaponType.Hammer:
                {
                    if (hammerRingPrefab == null) return;
                    var vfx = Instantiate(hammerRingPrefab);
                    vfx.Play(center);
                    break;
                }

            case WeaponType.Spear:
                {
                    if (spearStabPrefab == null) return;

                    Vector2 start = (Vector2)transform.position + dir * 0.2f;
                    Vector2 end = (Vector2)transform.position + dir * (slot.meleeRange + slot.meleeRadius + 0.55f);

                    var vfx = Instantiate(spearStabPrefab);
                    vfx.Play(start, end);
                    break;
                }

            case WeaponType.Sword:
                {
                    // Sword'a şimdilik "kısa, geniş slash" (spear stab prefab’ıyla da yapılabilir)
                    if (spearStabPrefab == null) return;

                    Vector2 right = new Vector2(-dir.y, dir.x);
                    Vector2 start = center - right * (slot.meleeRadius * 0.9f);
                    Vector2 end = center + right * (slot.meleeRadius * 0.9f);

                    var vfx = Instantiate(spearStabPrefab);
                    vfx.Play(start, end);
                    break;
                }
        }
    }


    void FireSniper(WeaponSlot slot)
    {
        float finalRange = slot.baseRange * (1f + rangeBonus);
        if (!TryGetDirectionToNearestInRange(finalRange, out Vector2 dir)) return;

        SpawnBullet(dir, slot.damage, slot.baseRange);
    }




    void FireRifle(WeaponSlot slot)
    {
        float finalRange = slot.baseRange * (1f + rangeBonus);
        if (!TryGetDirectionToNearestInRange(finalRange, out Vector2 dir)) return;

        SpawnBullet(dir, slot.damage, slot.baseRange);
    }



    void FireShotgun(WeaponSlot slot)
    {
        float finalRange = slot.baseRange * (1f + rangeBonus);
        if (!TryGetDirectionToNearestInRange(finalRange, out Vector2 dir)) return;

        int pellets = Mathf.Max(1, slot.pelletCount);

        if (pellets == 1)
        {
            SpawnBullet(dir, slot.damage, slot.baseRange);
            return;
        }

        float startAngle = -slot.spreadAngle / 2f;
        float step = slot.spreadAngle / (pellets - 1);

        for (int i = 0; i < pellets; i++)
        {
            float angle = startAngle + step * i;
            Vector2 rotatedDir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)dir);
            SpawnBullet(rotatedDir, slot.damage, slot.baseRange);
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
        NotifyHealthUI();
    }

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;


    public void ChangeArmor(int delta)
    {
        armor = Mathf.Clamp(armor + delta, 0, 999);
    }

    public void ChangeHpRegen(float delta)
    {
        hpRegenPerSec = Mathf.Max(0f, hpRegenPerSec + delta);
    }

    public void ChangePickupRange(float delta)
    {
        pickupRangeBonus = Mathf.Clamp(pickupRangeBonus + delta, 0f, 50f);
    }

    public void ChangeCritChance(float delta)
    {
        critChance = Mathf.Clamp(critChance + delta, 0f, 100f);
    }

    public void ChangeLuck(float delta)
    {
        luck = Mathf.Clamp(luck + delta, 0f, 100f);
    }

    public void ChangeRange(float delta)
    {
        rangeBonus = Mathf.Clamp(rangeBonus + delta, 0f, 3f);
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
            slot.baseRange = 7.0f;
        }
        else if (type == WeaponType.Shotgun)
        {
            slot.interval = 0.9f;
            slot.damage = 1;
            slot.pelletCount = 5;
            slot.spreadAngle = 35f;
            slot.baseRange = 5.0f;
        }
        else if (type == WeaponType.Sniper)
        {
            slot.interval = 1.25f;  // yavaş
            slot.damage = 4;        // yüksek
            slot.pelletCount = 1;
            slot.spreadAngle = 0f;
            slot.baseRange = 11.0f;
        }
        else if (type == WeaponType.Sword)
        {
            slot.interval = 0.40f;  // hızlı
            slot.damage = 2;
            slot.meleeRange = 1.5f;
            slot.meleeRadius = 0.55f;
            slot.maxTargets = 1;    // tek hedef hissi
        }
        else if (type == WeaponType.Spear)
        {
            slot.interval = 0.60f;  // orta
            slot.damage = 2;
            slot.meleeRange = 2f; // daha uzak
            slot.meleeRadius = 0.40f;
            slot.maxTargets = 2;    // delip 2 hedefe vurma hissi
        }
        else if (type == WeaponType.Hammer)
        {
            slot.interval = 0.95f;  // yavaş
            slot.damage = 3;
            slot.meleeRange = 1f;
            slot.meleeRadius = 0.90f; // geniş alan
            slot.maxTargets = 0;    // sınırsız (AoE)
        }

        return slot;
    }

    private void NotifyHealthUI()
    {
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public void HealToFull()
    {
        currentHp = maxHp;
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    private Coroutine hitFlashRoutine;

    void PlayHitFlash()
    {
        if (spriteRenderer == null) return;

        // Üst üste hit gelirse önceki flash'ı kes
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        // ✅ kritik: kırmızıda kalmışsa önce normale çek
        spriteRenderer.color = normalColor;

        hitFlashRoutine = StartCoroutine(HitFlashCoroutine());
    }

    private System.Collections.IEnumerator HitFlashCoroutine()
    {
        spriteRenderer.color = hitColor;

        // ✅ kritik: pause olsa bile bitsin (timescale 0 etkilenmez)
        yield return new WaitForSecondsRealtime(hitFlashDuration);

        // ✅ kritik: original değil, her zaman normale dön
        spriteRenderer.color = normalColor;

        hitFlashRoutine = null;
    }

    public void AddOwnedItem(string title, string desc, Sprite icon, int cost, int rarity)
    {
        ownedItems.Add(new OwnedItem
        {
            title = title,
            desc = desc,
            icon = icon,
            cost = cost,
            rarity = rarity
        });
    }

    public bool HasFreeWeaponSlot()
    {
        for (int i = 0; i < weaponSlots.Count; i++)
            if (!weaponSlots[i].isActive)
                return true;

        return false;
    }

    private void OnDisable() => ResetVisualState();
    private void OnEnable() => ResetVisualState();
}
