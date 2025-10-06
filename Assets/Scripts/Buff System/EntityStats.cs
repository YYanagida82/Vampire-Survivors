using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エンティティ（プレイヤー、敵など）のステータスとバフ効果を管理する抽象基本クラス。
/// このクラスを継承することで、バフシステムの共通機能を実装できる。
/// </summary>
public abstract class EntityStats : MonoBehaviour
{
    /// <summary>
    /// 現在の体力。
    /// </summary>
    public float health;

    // エンティティに付与する色とアニメーションを定義
    protected SpriteRenderer sprite;
    protected Animator animator;
    protected Color originalColor;
    protected List<Color> appliedTints = new List<Color>();
    public const float TINT_FACTOR = 4f;

    /// <summary>
    /// エンティティに適用されているアクティブなバフのインスタンスを表す内部クラス。
    /// </summary>
    [System.Serializable]
    public class Buff
    {
        /// <summary>
        /// バフの設計図となるデータ。
        /// </summary>
        public BuffData data;
        /// <summary>
        /// バフの残り効果時間。
        /// </summary>
        public float remainingDuration;
        /// <summary>
        /// 次の継続効果（DoT/HoT）が発生するまでの時間。
        /// </summary>
        public float nextTick;
        /// <summary>
        /// 適用されているバフのバリエーション（レベル）。
        /// </summary>
        public int variant;

        public ParticleSystem effect;   // バフ効果エフェクト
        public Color tint;  // バフに付与する色
        public float animationSpeed = 1f;   // アニメーションスピード

        public Buff(BuffData d, EntityStats owner, int variant = 0, float durationMultipier = 1f)
        {
            data = d;
            BuffData.Stats buffStats = d.Get(variant);
            remainingDuration = buffStats.duration * durationMultipier;
            nextTick = buffStats.tickInterval;
            this.variant = variant;

            if (buffStats.effect) effect = Instantiate(buffStats.effect, owner.transform);
            if (buffStats.tint.a > 0)
            {
                tint = buffStats.tint;
                owner.ApplyTint(buffStats.tint);
            }

            // アニメーションスピードの適用
            animationSpeed = buffStats.animationSpeed;
            owner.ApplyAnimationMultiplier(animationSpeed);
        }

        /// <summary>
        /// バフインスタンスのStatsデータを取得。
        /// </summary>
        public BuffData.Stats GetData()
        {
            return data.Get(variant);
        }
    }

    /// <summary>
    /// 現在適用されているすべてのアクティブなバフのリスト。
    /// </summary>
    protected List<Buff> activeBuffs = new List<Buff>();

    /// <summary>
    /// バフを適用する際の追加情報（適用確率など）をまとめたヘルパークラス。
    /// </summary>
    [System.Serializable]
    public class BuffInfo
    {
        public BuffData data;
        public int variant;
        [Range(0f, 1f)] public float probability = 1f;
    }

    protected virtual void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        originalColor = sprite.color;
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
    }

    /// <summary>
    /// アニメーションの再生速度に乗数を適用します。
    /// スロー効果やスタン効果に使用されます。
    /// </summary>
    /// <param name="factor">適用する乗数（0.5で半分の速度、2.0で倍速）</param>
    public virtual void ApplyAnimationMultiplier(float factor)
    {
        // factorが0の場合、完全に0を掛けると後で割り算しても元に戻せなくなる。
        // そのため、ほぼ0に近い非常に小さい値を代わりに使用して、事実上アニメーションを停止させる。
        animator.speed *= Mathf.Approximately(0, factor) ? 0.000001f : factor;
    }

    /// <summary>
    /// 適用したアニメーション速度の乗数を元に戻します。
    /// </summary>
    /// <param name="factor">適用時と同じ乗数</param>
    public virtual void RemoveAnimationMultiplier(float factor)
    {
        // 適用時と同じ値で割り算することで、元の速度に復元する。
        animator.speed /= Mathf.Approximately(0, factor) ? 0.000001f : factor;
    }

    public virtual void ApplyTint(Color c)
    {
        appliedTints.Add(c);
        UpdateColor();
    }

    public virtual void RemoveTint(Color c)
    {
        appliedTints.Remove(c);
        UpdateColor();
    }

    /// <summary>
    /// 適用されているすべてのティントカラーを合成し、スプライトの最終的な色を計算して更新します。
    /// 計算には、各ティントのアルファ値を重みとして使用する加重平均が用いられます。
    /// </summary>
    public virtual void UpdateColor()
    {
        // ターゲットの色を、ティントが適用される前の元の色で初期化する
        Color targetColor = originalColor;

        // 加重平均の計算のため、重みの合計を1で初期化する（元の色の重み）
        float totalWeight = 1f;

        // 適用されているすべてのティントをループ処理
        foreach (Color c in appliedTints)
        {
            // ティントの色と重み（アルファ値）をターゲットカラーに加算していく
            targetColor = new Color(
                targetColor.r + c.r * c.a * TINT_FACTOR,
                targetColor.g + c.g * c.a * TINT_FACTOR,
                targetColor.b + c.b * c.a * TINT_FACTOR,
                targetColor.a
            );
            // 重みの合計にも、このティントの重みを加算する
            totalWeight += c.a * TINT_FACTOR;
        }

        // 最終的な色を計算（加重平均）
        // 合計した色値を、重みの合計で割る
        targetColor = new Color(
            targetColor.r / totalWeight,
            targetColor.g / totalWeight,
            targetColor.b / totalWeight,
            targetColor.a
        );

        // 計算した色をスプライトに適用する
        sprite.color = targetColor;
    }

    /// <summary>
    /// 指定されたデータに一致するアクティブなバフを取得します。
    /// </summary>
    /// <param name="data">検索するBuffData。</param>
    /// <param name="variant">検索するバリエーション（レベル）。-1の場合は問わない。</param>
    /// <returns>見つかったバフ。なければnull。</returns>
    public virtual Buff GetBuff(BuffData data, int variant = -1)
    {
        foreach (Buff b in activeBuffs)
        {
            if (b.data == data)
            {
                if (variant >= 0)
                {
                    if (b.variant == variant) return b;
                }
                else
                {
                    return b;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 確率に基づいてバフを適用します。
    /// </summary>
    public virtual bool ApplyBuff(BuffInfo info, float durationMultipier = 1f)
    {
        if (info == null || info.data == null) return false;
        if (UnityEngine.Random.value <= info.probability)
            return ApplyBuff(info.data, info.variant, durationMultipier);
        return false;
    }

    /// <summary>
    /// バフをエンティティに適用します。スタックタイプに応じて挙動が変わります。
    /// </summary>
    /// <returns>バフが適用されたかどうか。</returns>
    public virtual bool ApplyBuff(BuffData data, int variant = 0, float durationMultipier = 1f)
    {
        Buff b;
        BuffData.Stats s = data.Get(variant);

        switch (s.stackType)
        {
            case BuffData.StackType.stacksFully:
                activeBuffs.Add(new Buff(data, this, variant, durationMultipier));
                RecalculateStats();
                return true;

            case BuffData.StackType.refreshDurationOnly:
                b = GetBuff(data, variant);
                if (b != null)
                {
                    b.remainingDuration = s.duration * durationMultipier;
                }
                else
                {
                    activeBuffs.Add(new Buff(data, this, variant, durationMultipier));
                    RecalculateStats();
                }
                return true;

            case BuffData.StackType.doesNotStack:
                b = GetBuff(data, variant);
                if (b != null)
                {
                    activeBuffs.Add(new Buff(data, this, variant, durationMultipier));
                    RecalculateStats();
                    return true;
                }
                return false;
        }
        return false;
    }

    /// <summary>
    /// 指定されたバフをエンティティから削除します。
    /// </summary>
    public virtual bool RemoveBuff(BuffData data, int variant = -1)
    {
        List<Buff> toRemove = new List<Buff>();
        foreach (Buff b in activeBuffs)
        {
            if (b.data == data)
            {
                if (variant >= 0)
                {
                    if (b.variant == variant) toRemove.Add(b);
                }
                else
                {
                    toRemove.Add(b);
                }
            }
        }

        if (toRemove.Count > 0)
        {
            foreach (Buff b in toRemove)
            {
                if (b.effect) ObjectPool.instance.Return(b.effect.gameObject);
                if (b.tint.a > 0) RemoveTint(b.tint);
                RemoveAnimationMultiplier(b.animationSpeed);
                activeBuffs.Remove(b);
            }
            RecalculateStats();
            return true;
        }
        return false;
    }

    // --- 派生クラスで必ず実装する抽象メソッド --- //

    /// <summary>
    /// ダメージを受ける処理。
    /// </summary>
    public abstract void TakeDamage(float dmg);
    /// <summary>
    /// 体力を回復する処理。
    /// </summary>
    public abstract void RestoreHealth(float amount);
    /// <summary>
    /// エンティティが死亡したときの処理。
    /// </summary>
    public abstract void Kill();
    /// <summary>
    /// 現在のアクティブなバフに基づいて最終的なステータスを再計算する処理。
    /// </summary>
    public abstract void RecalculateStats();

    /// <summary>
    /// 毎フレーム、バフの効果時間や継続効果を処理します。
    /// </summary>
    protected virtual void Update()
    {
        List<Buff> expired = new List<Buff>();
        foreach (Buff b in activeBuffs)
        {
            BuffData.Stats s = b.data.Get(b.variant);

            // 継続ダメージ/回復のティック処理
            b.nextTick -= Time.deltaTime;
            if (b.nextTick < 0)
            {
                float tickDmg = b.data.GetTickDamage(b.variant);
                if (tickDmg > 0) TakeDamage(tickDmg);
                float tickHeal = b.data.GetTickHeal(b.variant);
                if (tickHeal > 0) RestoreHealth(tickHeal);
                b.nextTick = s.tickInterval;
            }

            // 効果時間が無限でなければ、残り時間を減らす
            if (s.duration <= 0) continue;

            b.remainingDuration -= Time.deltaTime;
            if (b.remainingDuration < 0) expired.Add(b);
        }

        foreach (Buff b in expired)
        {
            if (b.effect) ObjectPool.instance.Return(b.effect.gameObject);
            if (b.tint.a > 0) RemoveTint(b.tint);
            RemoveAnimationMultiplier(b.animationSpeed);
            activeBuffs.Remove(b);
        }
        RecalculateStats();
    }
}