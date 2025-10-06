using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObjects/WeaponData")]
public class WeaponData : ItemData
{
    [HideInInspector] public string behaviour;
    public Weapon.Stats baseStats;
    public Weapon.Stats[] linearGrowth;
    public Weapon.Stats[] randomGrowth;

    // レベルに応じた武器ステータスを返す
    public override Item.LevelData GetLevelData(int level)
    {
        if (level <= 1) return baseStats;

        if (level - 2 < linearGrowth.Length)
            return linearGrowth[level - 2];

        if (randomGrowth.Length > 0)
            return randomGrowth[Random.Range(0, randomGrowth.Length)];

        return new Weapon.Stats();
    }
}
