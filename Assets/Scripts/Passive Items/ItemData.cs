using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public Sprite icon;
    public int maxLevel;

    // 進化条件を定義する構造体
    [System.Serializable]
    public struct Evolution
    {
        public string name;
        public enum Condition { auto, treasureChest }
        public Condition condition;

        [System.Flags] public enum Consumption { passive = 1, weapon = 2 }
        public Consumption consumes;

        public int evolutionLevel;
        public Config[] catalysts;
        public Config outcome;

        [System.Serializable]
        public struct Config
        {
            public ItemData itemType;
            public int level;
        }
    }

    public Evolution[] evolutionData;

    public abstract Item.LevelData GetLevelData(int level);
}
