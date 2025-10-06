using UnityEngine;

public abstract class Weapon : Item
{
    // 武器のステータスを保持する構造体
    [System.Serializable]
    public class Stats : LevelData
    {
        [Header("Visuals")]
        public Projectile projectilePrefab;
        public Aura auraPrefab;
        public ParticleSystem hitEffect;
        public Rect spawnVariance;

        [Header("Sounds")]
        public AudioClip hitSound;

        [Header("Values")]
        public float lifespan;
        public float damage, damageVariance, area, speed, cooldown, projectileInterval, knockback;
        public int number, piercing, maxInstances;

        public EntityStats.BuffInfo[] appliedBuffs;

        public static Stats operator +(Stats s1, Stats s2)
        {
            Stats result = new Stats();
            result.name = s2.name ?? s1.name;
            result.description = s2.description ?? s1.description;
            result.projectilePrefab = s2.projectilePrefab ?? s1.projectilePrefab;
            result.auraPrefab = s2.auraPrefab ?? s1.auraPrefab;
            result.hitEffect = s2.hitEffect == null ? s1.hitEffect : s2.hitEffect;
            result.spawnVariance = s2.spawnVariance;
            result.lifespan = s1.lifespan + s2.lifespan;
            result.damage = s1.damage + s2.damage;
            result.damageVariance = s1.damageVariance + s2.damageVariance;
            result.area = s1.area + s2.area;
            result.speed = s1.speed + s2.speed;
            result.cooldown = s1.cooldown + s2.cooldown;
            result.number = s1.number + s2.number;
            result.piercing = s1.piercing + s2.piercing;
            result.projectileInterval = s1.projectileInterval + s2.projectileInterval;
            result.knockback = s1.knockback + s2.knockback;
            result.appliedBuffs = s2.appliedBuffs == null || s2.appliedBuffs.Length <= 0 ? s1.appliedBuffs : s2.appliedBuffs;
            result.hitSound = s2.hitSound == null ? s1.hitSound : s2.hitSound;
            return result;
        }

        public float GetDamage()
        {
            return damage + Random.Range(0, damageVariance);
        }
    }

    protected Stats currentStats;
    protected float currentCooldown;
    protected PlayerMovement movement;

    // 武器データ初期化
    public virtual void Initialise(WeaponData data)
    {
        base.Initialise(data);
        this.data = data;
        currentStats = data.baseStats;
        movement = GetComponentInParent<PlayerMovement>();
        ActivateCooldown();
    }

    protected virtual void Update()
    {
        // ゲームオーバー中なら武器のクールダウン/攻撃ロジック全体を停止する
        if (GameManager.instance != null && GameManager.instance.isGameOver)
        {
            return; // これ以降のクールダウン計算や攻撃判定をスキップ
        }

        currentCooldown -= Time.deltaTime;
        if (currentCooldown <= 0f)
        {
            Attack(currentStats.number + owner.Stats.amount);
        }
    }

    public override bool DoLevelUp()
    {
        base.DoLevelUp();
        if (!CanLevelUp())
        {
            return false;
        }

        currentStats += (Stats)data.GetLevelData(++currentLevel);
        return true;
    }

    public virtual bool CanAttack()
    {
        // プレイヤーの攻撃力が0の場合は攻撃できない
        if (Mathf.Approximately(owner.Stats.might, 0)) return false;
        return currentCooldown <= 0;
    }

    protected virtual bool Attack(int attackCount = 1)
    {
        if (CanAttack())
        {
            ActivateCooldown();
            return true;
        }
        return false;
    }

    public virtual float GetDamage()
    {
        return currentStats.GetDamage() * owner.Stats.might;
    }

    public virtual float GetArea()
    {
        return currentStats.area * owner.Stats.area;
    }

    public virtual Stats GetStats() { return currentStats; }

    // 武器のクールダウンを更新（チャージアップ方式）
    public virtual bool ActivateCooldown(bool strict = false)
    {
        // strictモードが有効かつクールダウンが既に始まっている場合は何もしない（クールダウン中）
        if (strict && currentCooldown > 0) return false;

        // 最終的に適用されるクールダウン時間（目標値）を計算
        float actualCooldown = currentStats.cooldown * Owner.Stats.cooldown;

        // クールダウンを即座に目標値(actualCooldown)にセットする
        currentCooldown = Mathf.Min(actualCooldown, currentCooldown + actualCooldown);

        return true;
    }

    /// <summary>
    /// この武器に付与されているバフを対象のエンティティに適用します。
    /// </summary>
    /// <param name="e">バフを適用する対象のエンティティステータス</param>
    public void ApplyBuffs(EntityStats e)
    {
        if (GetStats().appliedBuffs == null) return;
        foreach (EntityStats.BuffInfo b in GetStats().appliedBuffs)
        {
            e.ApplyBuff(b, owner.Actual.duration);
        }
    }
}
