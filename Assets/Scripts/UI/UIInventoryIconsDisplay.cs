using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutGroup))]
public class UIInventoryIconsDisplay : MonoBehaviour
{
    public GameObject slotTemplate; // インベントリスロットのテンプレート
    public uint maxSlots = 6;   // 最大スロット数
    public bool showLevels = true;  // レベルを表示するかどうか
    public PlayerInventory inventory;   // プレイヤーのインベントリ

    public GameObject[] slots;  // インベントリスロットの配列

    [Header("Paths")]
    public string iconPath; // アイコンへのパス
    public string levelTextPath;    // レベルテキストへのパス
    [HideInInspector] public string targetedItemList; // 対象となるアイテムリストの変数名を指定する

    void Reset()
    {
        // 子オブジェクトの最初のものをスロットテンプレートとして設定
        slotTemplate = transform.GetChild(0).gameObject;
        // シーン内のPlayerInventoryを検索して設定
        inventory = FindFirstObjectByType<PlayerInventory>();
    }

    void OnEnable()
    {
        Refresh();  // 表示更新する
    }

    public void Refresh()
    {
        // インベントリが設定されていない場合は何もしない
        if (!inventory) return;

        // PlayerInventoryクラスの型情報を取得
        Type t = typeof(PlayerInventory);
        // targetedItemListで指定された名前のフィールド情報を取得
        FieldInfo field = t.GetField(targetedItemList, BindingFlags.Public | BindingFlags.Instance);

        // フィールドが見つからない場合は何もしない
        if (field == null) return;

        // 取得したフィールドからアイテムリストを取得
        List<PlayerInventory.Slot> items = (List<PlayerInventory.Slot>)field.GetValue(inventory);

        // アイテムリストをループして各スロットを更新
        for (int i = 0; i < items.Count; i++)
        {
            // スロット配列の範囲外になったらループを抜ける
            if (i >= slots.Length) break;

            // 現在のアイテムを取得
            Item item = items[i].item;

            // アイコンオブジェクトをパスから検索
            Transform iconObj = slots[i].transform.Find(iconPath);
            if (iconObj)
            {
                // アイコンのImageコンポーネントを取得
                Image icon = iconObj.gameObject.GetComponentInChildren<Image>();
                // アイテムがない場合はアイコンを非表示にする
                if (!item) icon.color = new Color(1, 1, 1, 0);
                else
                {
                    // アイテムがある場合はアイコンを表示し、スプライトを設定
                    icon.color = new Color(1, 1, 1, 1);
                    if (icon) icon.sprite = item.data.icon;
                }
            }

            // レベルテキストオブジェクトをパスから検索
            Transform levelObj = slots[i].transform.Find(levelTextPath);
            if (levelObj)
            {
                // レベルテキストのTextMeshProUGUIコンポーネントを取得
                TextMeshProUGUI levelText = levelObj.gameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (levelText)
                {
                    // アイテムがない、またはレベルを表示しない設定の場合はテキストを空にする
                    if (!item || !showLevels) levelText.text = "";
                    // それ以外の場合はアイテムの現在レベルを表示
                    else levelText.text = item.currentLevel.ToString();
                }
            }
        }
    }
}
