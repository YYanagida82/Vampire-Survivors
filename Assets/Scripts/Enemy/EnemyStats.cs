using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyStats : EntityStats
{
    [System.Serializable]
    public class Resistances
    {
        [Range(-1f, 1f)] public float freeze, kill, debuff;  // 凍結、即死、デバフ（弱体効果）への耐性

        public static Resistances operator *(Resistances r, float factor)
        {
            // 各耐性値に係数(factor)を掛け、上限を1に制限する
            r.freeze = Mathf.Min(1, r.freeze * factor);
            r.kill = Mathf.Min(1, r.kill * factor);
            r.debuff = Mathf.Min(1, r.debuff * factor);
            return r;
        }

        public static Resistances operator +(Resistances r1, Resistances r2)
        {
            r1.freeze += r2.freeze;
            r1.kill += r2.kill;
            r1.debuff += r2.debuff;
            return r1;
        }

        public static Resistances operator *(Resistances r1, Resistances r2)
        { 
            r1.freeze = Mathf.Min(1, r1.freeze * r2.freeze);
            r1.kill = Mathf.Min(1, r1.kill * r2.kill);
            r1.debuff = Mathf.Min(1, r1.debuff * r2.debuff);
            return r1;
        }
    }

    [System.Serializable]
    public struct Stats
    {
        // 最大体力、移動速度、攻撃力、ノックバック倍率、耐性
        public float maxHealth, moveSpeed, damage, knockbackMultiplier;
        public Resistances resistances;

        // どのステータスが強化可能かを示すためのフラグ
        [System.Flags]
        public enum Boostable { health = 1, moveSpeed = 2, damage = 4, knockbackMultiplier = 8, resistances = 16 }

        // 「呪い」「プレイヤーレベル」によって強化されるステータスをUnityエディタで設定するための構造体
        public Boostable curseBoosts, levelBoosts;

        //　ステータスを強化するメソッド
        private static Stats Boost(Stats s1, float factor, Boostable boostable)
        {
            if ((boostable & Boostable.health) != 0) s1.maxHealth *= factor;
            if ((boostable & Boostable.moveSpeed) != 0) s1.moveSpeed *= factor;
            if ((boostable & Boostable.damage) != 0) s1.damage *= factor;
            if ((boostable & Boostable.knockbackMultiplier) != 0) s1.knockbackMultiplier *= factor;
            if ((boostable & Boostable.resistances) != 0) s1.resistances *= factor;
            return s1;
        }

        public static Stats operator *(Stats s1, float factor) { return Boost(s1, factor, s1.curseBoosts); }

        public static Stats operator ^(Stats s1, float factor) { return Boost(s1, factor, s1.levelBoosts); }

        public static Stats operator +(Stats s1, Stats s2)
        {
            s1.maxHealth += s2.maxHealth;
            s1.moveSpeed += s2.moveSpeed;
            s1.damage += s2.damage;
            s1.knockbackMultiplier += s2.knockbackMultiplier;
            s1.resistances += s2.resistances;
            return s1;
        }

        public static Stats operator *(Stats s1, Stats s2)
        {
            s1.maxHealth *= s2.maxHealth;
            s1.moveSpeed *= s2.moveSpeed;
            s1.damage *= s2.damage;
            s1.knockbackMultiplier *= s2.knockbackMultiplier;
            s1.resistances *= s2.resistances;
            return s1;
        }
    }

    public Stats baseStats = new Stats { maxHealth = 10, moveSpeed = 1, damage = 3, knockbackMultiplier = 1};
    Stats actualStats;
    public Stats Actual
    {
        get { return actualStats; }
    }

    public BuffInfo[] attackEffects;

    [Header("Damage Feedback")]
    public Color damageColor = new Color(1, 0, 0, 1);
    public float damageFlashDuration = 0.2f;
    public float deathFadeTime = 0.6f;
    EnemyMovement movement;

    public static int count;
    public static List<EnemyStats> activeEnemies = new List<EnemyStats>();

    void OnEnable()
    {
        count++;
        activeEnemies.Add(this);

        // When the object is reused, reset its stats
        RecalculateStats();
        health = actualStats.maxHealth;

        // Clear leftover tints and restore original color
        appliedTints.Clear();
        UpdateColor();

        // 確実にコルーチンを停止（KillFade()などの残りをクリア）
        StopAllCoroutines(); 

        // EnemyMovementの状態をリセット
        if (TryGetComponent<EnemyMovement>(out EnemyMovement em))
        {
            em.ResetMovementState();
        }
    }

    protected override void Start()
    {
        base.Start();
        movement = GetComponent<EnemyMovement>();
    }

    // 凍結耐性とデバフ耐性の適用
    public override bool ApplyBuff(BuffData data, int variant = 0, float durationMultipier = 1f)
    {
        if ((data.type & BuffData.Type.freeze) > 0)
            if (Random.value <= Actual.resistances.freeze) return false;

        if ((data.type & BuffData.Type.debuff) > 0)
            if (Random.value <= Actual.resistances.debuff) return false;

        return base.ApplyBuff(data, variant, durationMultipier);
    }

    public override void RecalculateStats()
    {
        // 呪いブーストの適用
        float curse = GameManager.GetCumulativeCurse(),
              level = GameManager.GetCumulativeLevels();
        actualStats = (baseStats * curse) ^ level;

        // 乗算バフの効果を蓄積するためのStatsオブジェクトを初期化
        Stats multiplier = new Stats
        {
            maxHealth = 1f,
            moveSpeed = 1f,
            damage = 1f,
            knockbackMultiplier = 1,
            resistances = new Resistances { freeze = 1f, debuff = 1f, kill = 1f }
        };

        // 現在有効なすべてのバフをループ処理
        foreach (Buff b in activeBuffs)
        {
            BuffData.Stats bd = b.GetData();
            switch (bd.modifierType)
            {
                // 加算タイプの場合、ステータスに直接加算
                case BuffData.ModifierType.additive:
                    actualStats += bd.enemyModifier;
                    break;
                // 乗算タイプの場合、倍率を掛け合わせる
                case BuffData.ModifierType.multiplicative:
                    multiplier *= bd.enemyModifier;
                    break;
            }
        }

        // 最終的な乗算倍率を適用してステータスを更新
        actualStats *= multiplier;
    }

    public override void TakeDamage(float dmg)
    {
        // このオブジェクトが破棄済みでないか、実体が有効かを確認する(ゲームオーバー直前の不具合対策)
        if (this == null || gameObject == null || !gameObject.activeInHierarchy) return;

        health -= dmg;

        // 受けるダメージが最大HPと同じか(即死攻撃か)チェック
        if (dmg == actualStats.maxHealth)
        {
            // 一定の確率（kill抵抗値）でその攻撃を完全に無効化し、ダメージを受けないようにする
            if (Random.value < actualStats.resistances.kill) return;
        }

        // ダメージ表示
        if (dmg > 0)
        {
            StartCoroutine(DamageFlash());  // フラッシュ処理を開始
            GameManager.instance.GenerateFloationgText(Mathf.FloorToInt(dmg).ToString(), transform);
        }

        if (health <= 0)
        {
            Kill();
        }
    }

    public void TakeDamage(float dmg, Vector2 sourcePosition, float knockbackForce = 5f, float knockbackDuration = 0.2f)
    {
        TakeDamage(dmg);

        if (knockbackForce > 0)
        {
            // movementコンポーネントが有効か
            if (movement != null)
            {
                Vector2 dir = (Vector2)transform.position - sourcePosition; // ノックバック方向を計算
                movement.Knockback(dir.normalized * knockbackForce, knockbackDuration); // ノックバック開始
            }
        }
    }

    public override void RestoreHealth(float amount)
    {
        if (health < actualStats.maxHealth)
        {
            health += amount;
            if (health > actualStats.maxHealth)
            {
                health = actualStats.maxHealth;
            }    
        }
    }

    IEnumerator DamageFlash()
    {
        // 色を変更して一定時間待機してから元の色に戻す（点減効果）
        ApplyTint(damageColor);
        yield return new WaitForSeconds(damageFlashDuration);
        RemoveTint(damageColor);
    }

    public override void Kill()
    {
        DropRateManager drops = GetComponent<DropRateManager>();
        if (drops) drops.Drop();

        StartCoroutine(KillFade()); // フェードアウト
    }

    IEnumerator KillFade()
    {
        if (sprite == null)
        {
            // ゲームオーバー処理のタイミング次第ではspriteが先に破壊される可能性があるため、
            // spriteがない場合は即座にオブジェクトをプールに戻す
            if (ObjectPool.instance) ObjectPool.instance.Return(gameObject);
            else gameObject.SetActive(false);
            yield break; 
        }

        WaitForEndOfFrame w = new WaitForEndOfFrame();
        float t = 0, origAlpha = sprite.color.a;
        // フェードアウト
        while (t < deathFadeTime)
        {
            yield return w;
            t += Time.deltaTime;

            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, (1 - t / deathFadeTime) * origAlpha);
        }

        if (ObjectPool.instance)
        {
            ObjectPool.instance.Return(gameObject);
        }
        else
        {
            gameObject.SetActive(false); // フォールバック
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Mathf.Approximately(Actual.damage, 0)) return;  // ダメージステータスが0の敵は何もしない(凍結状態)

        if (other.TryGetComponent(out PlayerStats p))
        {
            p.TakeDamage(Actual.damage);
            foreach (BuffInfo b in attackEffects)
                p.ApplyBuff(b);
        }
    }

    private void OnDisable()
    {
        count--;
        activeEnemies.Remove(this);
    }
}
