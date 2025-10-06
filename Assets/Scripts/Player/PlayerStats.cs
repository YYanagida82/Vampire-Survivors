using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AppUI.UI;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : EntityStats
{
    CharacterData characterData;
    public CharacterData.Stats baseStats;
    [SerializeField] CharacterData.Stats actualStats;

    public CharacterData.Stats Stats
    {
        get { return actualStats; }
        set
        {
            actualStats = value;
        }
    }

    public CharacterData.Stats Actual
    {
        get { return actualStats; }
    }

    #region Current Stats Properties
    public float CurrentHealth
    {
        get { return health; }
        set
        {
            // 体力をリアルタイムに更新
            if (health != value)
            {
                health = value;
                UpdateHealthBar();
            }
        }
    }
    #endregion

    [Header("Visuals")]
    public ParticleSystem damageEffect; // ダメージエフェクト
    public ParticleSystem blockedEffect;    // アーマー装備時のエフェクト

    // 経験値とレベル
    [Header("Experience/Level")]
    public int experience = 0;
    public int level = 1;
    public int experienceCap;

    // レベル範囲とその範囲での経験値上限増加量を定義するクラス
    [System.Serializable]
    public class LevelRange
    {
        public int startLevel;  // 範囲の開始レベル
        public int endLevel;    // 範囲の終了レベル
        public int experienceCapIncrease;   // この範囲での経験値上限増加量
    }

    [Header("I-Frames")]
    public float invincibilityDuration; // 無敵時間の長さ
    float invincibilityTimer; // 無敵時間のタイマー
    bool isInvincible; // 無敵状態かどうか

    public List<LevelRange> levelRanges;

    PlayerCollector collector;
    PlayerInventory inventory; // インベントリマネージャーの参照

    [Header("UI")]
    public Image healthBar;
    public Image expBar;
    public TextMeshProUGUI levelText;

    [Header("Damage Feedback")]
    public Color damageColor = new Color(1, 0, 0, 1);
    public float damageFlashDuration = 0.2f;

    PlayerAnimator playerAnimator;

    protected override void Awake()
    {
        base.Awake();
        characterData = UICharacterSelector.GetData();    // UICharacterSelectorから選択されたキャラクターデータを取得

        inventory = GetComponent<PlayerInventory>();
        collector = GetComponentInChildren<PlayerCollector>();

        baseStats = actualStats = characterData.stats;  // キャラクターデータからステータスをコピー
        collector.SetRadius(actualStats.magnet);
        health = actualStats.maxHealth;

        playerAnimator = GetComponent<PlayerAnimator>();
        if(characterData.controller)
            playerAnimator.SetAnimatorController(characterData.controller); // アニメーション取得
    }

    protected override void Start()
    {
        base.Start();

        inventory.Add(characterData.StartingWeapon);    //  初期武器をインベントリに追加
        experienceCap = levelRanges[0].experienceCapIncrease; // 初期経験値上限を設定

        // リザルト画面UI初期化
        GameManager.instance.AssignChosenCharacterUI(characterData);

        UpdateHealthBar();
        UpdateExpBar();
        UpdateLevelText();
    }

    protected override void Update()
    {
        base.Update();

        // 無敵時間の管理
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
        }
        else if (isInvincible)
        {
            isInvincible = false; // 無敵状態を解除
        }
        Recover(); // 自動回復の呼び出し
    }

    public override void RecalculateStats()
    {
        actualStats = baseStats; // 基本ステータスをコピー

        // パッシブアイテムのスロットに入っていたらステータスを更新
        foreach (PlayerInventory.Slot s in inventory.passiveSlots)
        {
            Passive p = s.item as Passive;
            if (p)
            {
                actualStats += p.GetBoosts();    // ステータス上昇量を加算
            }
        }

        // キャラクターのレベルアップによるパッシブ効果を適用
        foreach (CharacterData.CurvePassive p in characterData.curvePassives)
        {
            float bonus = p.curve.Evaluate(level);
            switch (p.stat)
            {
                case "maxHealth": actualStats.maxHealth *= 1 + bonus; break;
                case "recovery": actualStats.recovery *= 1 + bonus; break;
                case "armor": actualStats.armor *= 1 + bonus; break;
                case "moveSpeed": actualStats.moveSpeed *= 1 + bonus; break;
                case "might": actualStats.might *= 1 + bonus; break;
                case "area": actualStats.area *= 1 + bonus; break;
                case "speed": actualStats.speed *= 1 + bonus; break;
                case "duration": actualStats.duration *= 1 + bonus; break;
                case "amount": actualStats.amount += (int)bonus; break; // amountは加算の可能性が高い
                case "cooldown": actualStats.cooldown *= 1 + bonus; break;
                case "luck": actualStats.luck *= 1 + bonus; break;
                case "growth": actualStats.growth *= 1 + bonus; break;
                case "greed": actualStats.greed *= 1 + bonus; break;
                case "curse": actualStats.curse *= 1 + bonus; break;
                case "magnet": actualStats.magnet *= 1 + bonus; break;
                case "revival": actualStats.revival += (int)bonus; break; // revivalは加算
            }
        }

        // 乗算バフの効果を蓄積するためのStatsオブジェクトを初期化
        CharacterData.Stats multiplier = new CharacterData.Stats
        {
            maxHealth = 1f,
            recovery = 1f,
            armor = 1f,
            moveSpeed = 1f,
            might = 1f,
            area = 1f,
            speed = 1f,
            duration = 1f,
            amount = 1,
            cooldown = 1f,
            luck = 1f,
            growth = 1f,
            greed = 1f,
            curse = 1f,
            magnet = 1f,
            revival = 1
        };

        // 現在有効なすべてのバフをループ処理
        foreach (Buff b in activeBuffs)
        {
            BuffData.Stats bd = b.GetData();
            switch (bd.modifierType)
            {
                // 加算タイプの場合、ステータスに直接加算
                case BuffData.ModifierType.additive:
                    actualStats += bd.playerModifier;
                    break;
                // 乗算タイプの場合、倍率を掛け合わせる
                case BuffData.ModifierType.multiplicative:
                    multiplier *= bd.playerModifier;
                    break;
            }
        }

        // 最終的な乗算倍率を適用してステータスを更新
        actualStats *= multiplier;

        collector.SetRadius(actualStats.magnet);
    }

    void LevelUpChecker()
    {
        // 経験値が上限に達した場合、レベルアップ
        if (experience >= experienceCap)
        {
            level++;
            experience -= experienceCap;    // 残りの経験値を保持

            // 新しいレベルに基づいて経験値上限を更新
            int experienceCapIncrease = 0;
            foreach (LevelRange range in levelRanges)
            {
                // 現在のレベルが範囲内にあるか確認
                if (level >= range.startLevel && level <= range.endLevel)
                {
                    experienceCapIncrease = range.experienceCapIncrease; // 経験値上限増加量を取得
                    break;
                }
            }
            experienceCap += experienceCapIncrease; // 経験値上限を更新

            UpdateLevelText(); // レベルテキストの更新

            GameManager.instance.StartLevelUp(); // レベルアップ開始

            // 大量の経験値で複数回のレベルアップをした場合
            if (experience >= experienceCap)
                LevelUpChecker(); // 再帰呼び出し
        }
    }

    void UpdateExpBar()
    {
        if (healthBar != null)
        { 
            expBar.fillAmount = (float)experience / experienceCap;
        }
    }

    void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "LV " + level.ToString();
        }
    }

    IEnumerator DamageFlash()
    {
        // 色を変更して一定時間待機してから元の色に戻す（点減効果）
        ApplyTint(damageColor);
        yield return new WaitForSeconds(damageFlashDuration);
        RemoveTint(damageColor);
    }

    public override void TakeDamage(float dmg)
    {
        // 無敵状態でなければダメージを受ける
        if (!isInvincible)
        {
            dmg -= actualStats.armor;
            if (dmg > 0)
            {
                CurrentHealth -= dmg;

                // ダメージエフェクトの生成
                if (damageEffect)
                { 
                    StartCoroutine(DamageFlash());  // フラッシュ処理を開始
                    ObjectPool.instance.Get(damageEffect.gameObject, transform.position, Quaternion.identity);
                }

                if (CurrentHealth <= 0) Kill();
            }
            else
            {
                if (blockedEffect) ObjectPool.instance.Get(blockedEffect.gameObject, transform.position, Quaternion.identity);
            }

            UpdateHealthBar();  // 体力バーの更新

            // 無敵状態にする
            invincibilityTimer = invincibilityDuration;
            isInvincible = true;
        }
    }

    void UpdateHealthBar()
    {
        if (expBar != null)
        {
            healthBar.fillAmount = CurrentHealth / actualStats.maxHealth;
        }
    }

    public override void Kill()
    {
        if (!GameManager.instance.isGameOver)
        {
            GameManager.instance.AssignLevelReachedUI(level);
            GameManager.instance.GameOver();
        }
    }

    // アイテム取得の際の体力回復メソッド
    public override void RestoreHealth(float amount)
    {
        // 体力を回復、最大体力を超えないようにする
        if (CurrentHealth < actualStats.maxHealth)
        {
            CurrentHealth += amount;
            if (CurrentHealth > actualStats.maxHealth)
            {
                CurrentHealth = actualStats.maxHealth;
            }

            UpdateHealthBar();
        }
    }
    public void IncreaseExperience(int amount)
    {
        // 経験値を増加させ、レベルアップをチェック
        experience += amount;
        LevelUpChecker();

        UpdateExpBar(); // 経験値バーの更新
    }

    // 自動回復メソッド
    void Recover()
    {
        if (CurrentHealth < actualStats.maxHealth)
        {
            CurrentHealth += Stats.recovery * Time.deltaTime;
            if (CurrentHealth > actualStats.maxHealth)
            {
                CurrentHealth = actualStats.maxHealth;
            }

            UpdateHealthBar();
        }
    }
}
