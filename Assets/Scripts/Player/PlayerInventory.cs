using System;
using System.Collections.Generic;
using NUnit.Compatibility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Item item;

        public void Assign(Item assignedItem)
        {
            item = assignedItem;
            if (item is Weapon)
            {
                Weapon w = item as Weapon;
            }
            else
            {
                Passive p = item as Passive;
            }
        }

        public void Clear()
        {
            item = null;
        }

        public bool IsEmpty() { return item == null; }
    }
    public List<Slot> weaponSlots = new List<Slot>(6);
    public List<Slot> passiveSlots = new List<Slot>(6);
    public UIInventoryIconsDisplay weaponUI, passiveUI;

    [Header("UI Elements")]
    public List<WeaponData> availableWeapons = new List<WeaponData>();
    public List<PassiveData> availablePassives = new List<PassiveData>();
    public UIUpgradeWindow upgradeWindow;

    PlayerStats player;

    void Start()
    {
        player = GetComponent<PlayerStats>();
    }

    // インベントリに特定のアイテムがあるか確認
    public bool Has(ItemData type) { return Get(type); }
    public Item Get(ItemData type)
    {
        if (type is WeaponData) return Get(type as WeaponData);
        else if (type is PassiveData) return Get(type as PassiveData);
        return null;
    }

    // インベントリに特定のパッシブアイテムがあるか確認
    public Passive Get(PassiveData type)
    {
        foreach (Slot s in passiveSlots)
        {
            Passive p = s.item as Passive;
            if (p && p.data == type) return p;
        }
        return null;
    }

    // インベントリに特定の武器があるか確認
    public Weapon Get(WeaponData type)
    {
        foreach (Slot s in weaponSlots)
        {
            Weapon w = s.item as Weapon;
            if (w && w.data == type) return w;
        }
        return null;
    }

    // dataで指定された特定武器のタイプを削除
    public bool Remove(WeaponData data, bool removeUpgradeAvailability = false)
    {
        if (removeUpgradeAvailability) availableWeapons.Remove(data);

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            Weapon w = weaponSlots[i].item as Weapon;
            if (w.data == data)
            {
                weaponSlots[i].Clear();
                w.OnUnequip();
                Destroy(w.gameObject);
                return true;
            }
        }

        return false;
    }

    public bool Remove(PassiveData data, bool removeUpgradeAvailability = false)
    {
        if (removeUpgradeAvailability) availablePassives.Remove(data);

        for (int i = 0; i < passiveSlots.Count; i++)
        {
            Passive p = passiveSlots[i].item as Passive;
            if (p.data == data)
            {
                passiveSlots[i].Clear();
                p.OnUnequip();
                Destroy(p.gameObject);
                return true;
            }
        }

        return false;
    }

    public bool Remove(ItemData data, bool removeUpgradeAvailability = false)
    {
        if (data is WeaponData) return Remove(data as WeaponData, removeUpgradeAvailability);
        else if (data is PassiveData) return Remove(data as PassiveData, removeUpgradeAvailability);
        return false;
    }

    public int Add(WeaponData data)
    {
        int slotNum = -1;

        // 空いているスロットを探す
        for (int i = 0; i < weaponSlots.Capacity; i++)
        {
            if (weaponSlots[i].IsEmpty())
            {
                slotNum = i;
                break;
            }
        }

        if (slotNum < 0) return slotNum;

        Type weaponType = Type.GetType(data.behaviour);

        if (weaponType != null)
        {
            // 武器のゲームオブジェクト生成
            GameObject go = new GameObject(data.baseStats.name);
            Weapon spawnedWeapon = (Weapon)go.AddComponent(weaponType);
            spawnedWeapon.transform.SetParent(transform);
            spawnedWeapon.transform.localPosition = Vector2.zero;
            spawnedWeapon.Initialise(data);
            spawnedWeapon.OnEquip();

            weaponSlots[slotNum].Assign(spawnedWeapon); // 武器スロットへ割り当てる
            weaponUI.Refresh();

            // レベルアップUIを閉じる
            if (GameManager.instance != null && GameManager.instance.choosingUpgrade)
            {
                GameManager.instance.EndLevelUp();
            }

            return slotNum;
        }

        return -1;
    }

    public int Add(PassiveData data)
    {
        int slotNum = -1;

        // 空いているスロットを探す
        for (int i = 0; i < passiveSlots.Capacity; i++)
        {
            if (passiveSlots[i].IsEmpty())
            {
                slotNum = i;
                break;
            }
        }

        if (slotNum < 0) return slotNum;

        GameObject go = new GameObject(data.baseStats.name);
        Passive p = go.AddComponent<Passive>();
        p.Initialise(data);
        p.transform.SetParent(transform);
        p.transform.localPosition = Vector2.zero;

        passiveSlots[slotNum].Assign(p); // パッシブアイテムスロットへ割り当てる
        passiveUI.Refresh();

        if (GameManager.instance != null && GameManager.instance.choosingUpgrade)
        {
            GameManager.instance.EndLevelUp();
        }
        player.RecalculateStats();

        return slotNum;
    }

    public int Add(ItemData data)
    {
        if (data is WeaponData) return Add(data as WeaponData);
        else if (data is PassiveData) return Add(data as PassiveData);
        return -1;
    }

    public bool LevelUp(ItemData data)
    {
        Item item = Get(data);
        if (item) return LevelUp(item);
        return false;
    }

    public bool LevelUp(Item item)
    {
        if (!item.DoLevelUp()) return false;    // 最大レベルまで達していたらレベルアップしない

        weaponUI.Refresh();
        passiveUI.Refresh();

        // レベルアップUIを閉じる
        if (GameManager.instance != null && GameManager.instance.choosingUpgrade)
            GameManager.instance.EndLevelUp();

        if(item is Passive) player.RecalculateStats();
        return true;
    }

    // 空きスロットの数
    int GetSlotsLeft(List<Slot> slots)
    {
        int count = 0;
        foreach (Slot s in slots)
        {
            if (s.IsEmpty()) count++;
        }
        return count;
    }

    void ApplyUpgradeOptions()
    {
        // 取得可能なすべてのアップグレードのリストを作成
        List<ItemData> availableUpgrades = new List<ItemData>();
        // すべての利用可能な武器とパッシブアイテムを一つのリストにまとめる
        List<ItemData> allUpgrades = new List<ItemData>(availableWeapons);
        allUpgrades.AddRange(availablePassives);

        // 武器とパッシブアイテムの空きスロット数を取得
        int weaponSlotsLeft = GetSlotsLeft(weaponSlots);
        int passiveSlotsLeft = GetSlotsLeft(passiveSlots);

        // すべてのアップグレード候補をループし、現在取得可能なものを絞り込む
        foreach (ItemData data in allUpgrades)
        {
            Item obj = Get(data);
            if (obj)  // 既にアイテムを所持している場合
            {
                // アイテムが最大レベル未満であれば、レベルアップ候補として追加
                if (obj.currentLevel < data.maxLevel) availableUpgrades.Add(data);
            }
            else  // アイテムを所持していない場合
            {
                // 武器であり、武器スロットに空きがあれば、新規取得候補として追加
                if (data is WeaponData && weaponSlotsLeft > 0) availableUpgrades.Add(data);
                // パッシブアイテムであり、パッシブスロットに空きがあれば、新規取得候補として追加
                else if (data is PassiveData && passiveSlotsLeft > 0) availableUpgrades.Add(data);
            }
        }

        // 実際に提示可能なアップグレードの数を取得
        int availUpgradeCount = availableUpgrades.Count;
        // 提示可能なアップグレードが1つ以上あるか確認
        if (availUpgradeCount > 0)
        {
            // プレイヤーの運(luck)ステータスに基づいて、追加の選択肢（4つ目）を表示するか決定
            // 運が高いほど、`getExtraItem`がtrueになる確率が上がる
            bool getExtraItem = 1f - 1f / player.Stats.luck > UnityEngine.Random.value;

            // 追加の選択肢を得られた、または提示可能なアップグレードが4つ未満の場合
            if (getExtraItem || availUpgradeCount < 4)
            {
                // 選択肢を4つ表示する
                upgradeWindow.SetUpgrades(this, availableUpgrades, 4);
            }
            else
            {
                // 通常通り、選択肢を3つ表示し、4つ目の選択肢のヒントをツールチップとして表示
                upgradeWindow.SetUpgrades(this, availableUpgrades, 3, "運の能力値を上げると確率で選択肢が1つ増える");
            }
        }
        // 提示可能なアップグレードがなく、現在アップグレード選択中の場合
        else if (GameManager.instance != null && GameManager.instance.choosingUpgrade)
        {
            // レベルアップ処理を終了する（例：宝箱を開けたが中身がなかった場合など）
            GameManager.instance.EndLevelUp();
        }
    }

    public void RemoveAndApplyUpgrades()
    {
        ApplyUpgradeOptions();
    }
}
