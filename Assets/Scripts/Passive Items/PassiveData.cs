using UnityEngine;

[CreateAssetMenu(fileName = "PassiveData", menuName = "ScriptableObjects/PassiveData")]
public class PassiveData : ItemData
{
    public Passive.Modifier baseStats;
    public Passive.Modifier[] growth;

    public override Item.LevelData GetLevelData(int level)
    {
        if (level <= 1) return baseStats;

        if (level - 2 < growth.Length)
            return growth[level - 2];

        return new Passive.Modifier();
    }
}
