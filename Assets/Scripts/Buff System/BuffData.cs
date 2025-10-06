using UnityEngine;

/// <summary>
/// バフ・デバフの様々な効果を定義するためのデータテンプレートです
/// 例：「攻撃力アップ」「毒」「移動速度ダウン」など
/// </summary>
[CreateAssetMenu(fileName = "BuffData", menuName = "ScriptableObjects/BuffData")]
public class BuffData : ScriptableObject
{
    /// <summary>
    /// バフの名前（UI表示用など）
    /// </summary>
    public new string name = "New Buff";

    /// <summary>
    /// バフのアイコン（UI表示用など）
    /// </summary>
    public Sprite icon;

    /// <summary>
    /// バフの分類。Flags属性により複数のタイプを組み合わせることが可能（例：強力なデバフ）
    /// </summary>
    [System.Flags]
    public enum Type : byte { buff = 1, debuff = 2, freeze = 4, strong = 8 }
    public Type type;

    /// <summary>
    /// 同じバフが複数回かかったときの挙動を定義
    /// </summary>
    public enum StackType : byte
    {
        /// <summary>効果は累積せず、効果時間のみをリフレッシュする</summary>
        refreshDurationOnly,
        /// <summary>効果も効果時間もすべて累積する（例：毒のスタック）</summary>
        stacksFully,
        /// <summary>スタックしない（すでに効果がある場合、新しいものは無視される）</summary>
        doesNotStack
    }

    /// <summary>
    /// ステータス変更の計算方法を定義
    /// </summary>
    public enum ModifierType : byte
    {
        /// <summary>加算（例：攻撃力+10）</summary>
        additive,
        /// <summary>乗算（例：攻撃力*1.2倍）</summary>
        multiplicative
    }

    /// <summary>
    /// バフの具体的な効果やレベルごとの違いを定義する内部クラス
    /// </summary>
    [System.Serializable]
    public class Stats
    {
        /// <summary>
        /// このバフレベルの名前（例：「Lv 1」）。
        /// </summary>
        public string name;

        [Header("Visuals")]
        [Tooltip("バフ効果中に、対象のゲームオブジェクトに表示するパーティクルエフェクト")]
        public ParticleSystem effect;
        [Tooltip("バフ効果中に、対象のスプライトに適用する色合い")]
        public Color tint = new Color(0, 0, 0, 0);
        [Tooltip("バフ効果中のアニメーション速度の倍率（1未満でスロー、1より大きいと高速化）")]
        public float animationSpeed = 1f;

        [Header("Stats")]
        /// <summary>
        /// バフの持続時間（秒）
        /// </summary>
        public float duration;
        /// <summary>
        /// 1秒あたりのダメージ量（継続ダメージ）
        /// </summary>
        public float damagePerSecond;
        /// <summary>
        /// 1秒あたりの回復量（継続回復）
        /// </summary>
        public float healPerSecond;

        [Tooltip("継続ダメージ/回復が発生する間隔（秒）")]
        public float tickInterval = 0.5f;

        /// <summary>
        /// このバフのスタック挙動
        /// </summary>
        public StackType stackType;
        /// <summary>
        /// このバフのステータス計算方法
        /// </summary>
        public ModifierType modifierType;

        public Stats()
        {
            duration = 10f;
            damagePerSecond = 1f;
            healPerSecond = 1f;
            tickInterval = 0.25f;
        }

        /// <summary>
        /// プレイヤーのステータスに対する変更内容
        /// </summary>
        public CharacterData.Stats playerModifier;
        /// <summary>
        /// 敵のステータスに対する変更内容
        /// </summary>
        public EnemyStats.Stats enemyModifier;
    }

    /// <summary>
    /// バフのレベルやバリエーションを定義する配列
    /// これにより、1つのBuffDataアセットで「攻撃力アップ Lv1」「攻撃力アップ Lv2」などを管理できる
    /// </summary>
    public Stats[] variations = new Stats[1] {
        new Stats { name = "Lv 1"}
    };

    /// <summary>
    /// 1ティックあたりのダメージ量を計算して返すヘルパーメソッド
    /// </summary>
    /// <param name="variant">バフのバリエーション（レベル）</param>
    /// <returns>1ティックあたりのダメージ量</returns>
    public float GetTickDamage(int variant = 0)
    {
        Stats s = Get(variant);
        return s.damagePerSecond * s.tickInterval;
    }

    /// <summary>
    /// 1ティックあたりの回復量を計算して返すヘルパーメソッド
    /// </summary>
    /// <param name="variant">バフのバリエーション（レベル）</param>
    /// <returns>1ティックあたりの回復量</returns>
    public float GetTickHeal(int variant = 0)
    {
        Stats s = Get(variant);
        return s.healPerSecond * s.tickInterval;
    }

    /// <summary>
    /// 指定されたバリエーション（レベル）のStatsデータを安全に取得するヘルパーメソッド
    /// </summary>
    /// <param name="variant">取得したいバリエーション（レベル）</param>
    /// <returns>バフのStatsデータ</returns>
    public Stats Get(int variant = -1)
    {
        return variations[Mathf.Max(0, variant)];
    }
}