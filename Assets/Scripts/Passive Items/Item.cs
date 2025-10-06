using System.Collections.Generic;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public int currentLevel = 1, maxLevel = 1;
    [HideInInspector] public ItemData data;
    protected ItemData.Evolution[] evolutionData;
    protected PlayerInventory inventory;
    protected PlayerStats owner;

    public PlayerStats Owner { get { return owner; } }

    [System.Serializable]
    public class LevelData
    {
        public string name, description;
    }

    public virtual void Initialise(ItemData data)
    {
        maxLevel = data.maxLevel;
        evolutionData = data.evolutionData;
        inventory = FindFirstObjectByType<PlayerInventory>();
        owner = FindFirstObjectByType<PlayerStats>();
    }

    // 進化可能なものをすべて返す
    public virtual ItemData.Evolution[] CanEvolve()
    {
        List<ItemData.Evolution> possibleEvolutions = new List<ItemData.Evolution>();

        // アイテムの全進化パターンをチェック
        foreach (ItemData.Evolution e in evolutionData)
        {
            // 進化可能であればリストに追加
            if (CanEvolve(e)) possibleEvolutions.Add(e);
        }

        return possibleEvolutions.ToArray();
    }

    // 進化可能かチェック
    public virtual bool CanEvolve(ItemData.Evolution evolution, int levelUpAmount = 1)
    {
        // アイテム自身のレベルが、進化に必要なレベルに達しているか確認
        if (evolution.evolutionLevel > currentLevel + levelUpAmount) return false;

        // 進化に必要なアイテムをすべて所持しているか確認
        foreach (ItemData.Evolution.Config c in evolution.catalysts)
        {
            Item item = inventory.Get(c.itemType);  // インベントリからアイテムを取得
            // 持っていない、要求レベルに達していない場合は進化できない
            if (item == null || item.currentLevel < c.level) return false;
        }

        return true;
    }

    // 進化処理実行
    public virtual bool AttemptEvolution(ItemData.Evolution evolutionData, int levelUpAmount = 1)
    {
        // 進化不可なら終了
        if (!CanEvolve(evolutionData, levelUpAmount)) return false;

        // 進化設定に応じて、素材となった武器やパッシブを消すかどうかを決める
        bool consumePassives = (evolutionData.consumes & ItemData.Evolution.Consumption.passive) > 0;
        bool consumeWeapons = (evolutionData.consumes & ItemData.Evolution.Consumption.weapon) > 0;

        // 進化素材のアイテムをインベントリから削除
        foreach (ItemData.Evolution.Config c in evolutionData.catalysts)
        {
            if (c.itemType is PassiveData && consumePassives) inventory.Remove((this as Passive).data, true);
            if (c.itemType is WeaponData && consumeWeapons) inventory.Remove((this as Weapon).data, true);
        }

        // 進化元のアイテムを削除
        if (this is Passive && consumePassives) inventory.Remove((this as Passive).data, true);
        else if (this is Weapon && consumeWeapons) inventory.Remove((this as Weapon).data, true);

        inventory.Add(evolutionData.outcome.itemType);  // 進化後のアイテムをインベントリに追加

        return true;
    }

    // レベルアップチェック
    public virtual bool CanLevelUp()
    {
        return currentLevel <= maxLevel;
    }

    // レベルアップ処理実行
    public virtual bool DoLevelUp()
    {
        // 進化先がなければ何もしない
        if (evolutionData == null) return false;

        // 全進化パターンをチェック
        foreach (ItemData.Evolution e in evolutionData)
        {
            if (e.condition == ItemData.Evolution.Condition.auto)
                AttemptEvolution(e);
        }
        return true;
    }

    public virtual void OnEquip() { }
    
    public virtual void OnUnequip() { }
}
