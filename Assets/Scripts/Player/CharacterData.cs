using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObjects/CharacterData")]
public class CharacterData : ScriptableObject
{
    // 変数名と日本語表示名の対応表
    private static readonly Dictionary<string, string> statDisplayNames = new Dictionary<string, string>
    {
        { "maxHealth", "最大HP" },
        { "recovery", "回復力" },
        { "armor", "防御力" },
        { "moveSpeed", "素早さ" },
        { "might", "攻撃力" },
        { "area", "攻撃範囲" },
        { "speed", "弾速" },
        { "duration", "効果時間" },
        { "amount", "発射段数" },
        { "cooldown", "クールダウン" },
        { "luck", "運" },
        { "growth", "成長効率" },
        { "greed", "欲深さ" },
        { "curse", "呪い" },
        { "magnet", "回収範囲" },
        { "revival", "復活" }
    };

    // ステータスの変数名から日本語の表示名を取得する関数
    public static string GetStatDisplayName(string fieldName)
    {
        // 変換リストに変数名があれば、対応する日本語名を返す
        if (statDisplayNames.TryGetValue(fieldName, out string displayName))
        {
            return displayName;
        }
        // なければ、元の変数名をそのまま返す
        return fieldName;
    }

    [SerializeField]
    Sprite icon;    // アイコン
    public Sprite Icon { get => icon; private set => icon = value; }

    public RuntimeAnimatorController controller;    // アニメーションコントローラー

    [SerializeField]
    new string name;    // キャラクター名
    public string Name { get => name; private set => name = value; }

    [SerializeField]
    string fullName;
    public string FullName { get => fullName; private set => fullName = value; }

    [SerializeField]
    [TextArea]
    string characterDescription;
    public string CharacterDescription { get => characterDescription; private set => characterDescription = value; }

    [SerializeField]
    WeaponData startingWeapon;    // 初期武器
    public WeaponData StartingWeapon { get => startingWeapon; private set => startingWeapon = value; }

    [System.Serializable]
    public class CurvePassive
    {
        public string stat;
        public AnimationCurve curve;
    }
    public List<CurvePassive> curvePassives = new List<CurvePassive>();

    [System.Serializable]
    public struct Stats
    {
        public float maxHealth, recovery, armor;
        [Range(-1, 10)] public float moveSpeed, might, area;
        [Range(-1, 5)] public float speed, duration;
        [Range(-1, 10)] public int amount;
        [Range(-1, 1)] public float cooldown;
        [Min(-1)] public float luck, growth, greed, curse;
        public float magnet;
        public int revival; // 復活回数

        public static Stats operator +(Stats s1, Stats s2)
        {
            s1.maxHealth += s2.maxHealth;
            s1.recovery += s2.recovery;
            s1.armor += s2.armor;
            s1.moveSpeed += s2.moveSpeed;
            s1.might += s2.might;
            s1.area += s2.area;
            s1.speed += s2.speed;
            s1.duration += s2.duration;
            s1.amount += s2.amount;
            s1.cooldown += s2.cooldown;
            s1.luck += s2.luck;
            s1.growth += s2.growth;
            s1.greed += s2.greed;
            s1.curse += s2.curse;
            s1.magnet += s2.magnet;
            s1.revival += s2.revival;
            return s1;
        }

        public static Stats operator *(Stats s1, Stats s2)
        {
            s1.maxHealth *= s2.maxHealth;
            s1.recovery *= s2.recovery;
            s1.armor *= s2.armor;
            s1.moveSpeed *= s2.moveSpeed;
            s1.might *= s2.might;
            s1.area *= s2.area;
            s1.speed *= s2.speed;
            s1.duration *= s2.duration;
            s1.amount *= s2.amount;
            s1.cooldown *= s2.cooldown;
            s1.luck *= s2.luck;
            s1.growth *= s2.growth;
            s1.greed *= s2.greed;
            s1.curse *= s2.curse;
            s1.magnet *= s2.magnet;
            return s1;
        }
    }
    public Stats stats = new Stats
    {
        maxHealth = 100,
        moveSpeed = 1,
        might = 1,
        amount = 0,
        area = 1,
        speed = 1,
        duration = 1,
        cooldown = 1,
        luck = 1,
        growth = 1,
        greed = 1,
        curse = 1
    };
}
