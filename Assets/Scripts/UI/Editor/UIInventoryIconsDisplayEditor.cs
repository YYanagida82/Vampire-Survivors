using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// UIInventoryIconsDisplayクラスのカスタムエディタを定義
[CustomEditor(typeof(UIInventoryIconsDisplay))]
public class UIInventoryIconsDisplayEditor : Editor
{
    // 編集対象のUIInventoryIconsDisplayコンポーネント
    UIInventoryIconsDisplay display;
    // 選択されているアイテムリストのインデックス
    int targetedItemListIndex = 0;
    // PlayerInventory内のアイテムリストの選択肢
    string[] itemListOptions;

    // エディタが有効になったときに呼び出される
    private void OnEnable()
    {
        // 編集対象のコンポーネントを取得
        display = target as UIInventoryIconsDisplay;

        // PlayerInventoryクラスの型情報を取得
        Type playerInventoryType = typeof(PlayerInventory);

        // PlayerInventoryクラスのすべてのフィールド（public, non-public, instance）を取得
        FieldInfo[] fields = playerInventoryType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // PlayerInventory.SlotのList<>であるフィールドの名前を抽出
        List<string> slotListNames = fields
            .Where(field => field.FieldType.IsGenericType &&
                             field.FieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                             field.FieldType.GetGenericArguments()[0] == typeof(PlayerInventory.Slot))
            .Select(field => field.Name)
            .ToList();

        // 選択肢の最初に "None" を追加
        slotListNames.Insert(0, "None");
        // 選択肢を配列に変換
        itemListOptions = slotListNames.ToArray();

        // 現在設定されているアイテムリストが選択肢の何番目にあるかを探し、インデックスを設定
        targetedItemListIndex = Math.Max(0, Array.IndexOf(itemListOptions, display.targetedItemList));
    }

    // インスペクタのGUIを描画する際に呼び出される
    public override void OnInspectorGUI()
    {
        // デフォルトのインスペクタGUIを描画
        base.OnInspectorGUI();

        // GUIの変更を監視開始
        EditorGUI.BeginChangeCheck();

        // アイテムリストを選択するためのポップアップメニューを表示
        targetedItemListIndex = EditorGUILayout.Popup(
            "Targeted Item List",
            Mathf.Max(0, targetedItemListIndex),
            itemListOptions
        );

        // GUIの変更があったかチェック
        if (EditorGUI.EndChangeCheck())
        {
            // 変更があった場合、選択されたアイテムリスト名を設定し、変更を保存
            display.targetedItemList = itemListOptions[targetedItemListIndex].ToString();
            EditorUtility.SetDirty(display);
        }

        // "Generate Icons"ボタンが押されたらRegenerateIconsメソッドを呼び出す
        if (GUILayout.Button("Generate Icons")) RegenerateIcons();
    }

    // アイコンを再生成する
    void RegenerateIcons()
    {
        // 編集対象のコンポーネントを再取得
        display = target as UIInventoryIconsDisplay;

        // Undo操作を登録
        Undo.RegisterCompleteObjectUndo(display, "Regenerate Icons");

        // 既存のスロットがあれば削除
        if (display.slots.Length > 0)
        {
            foreach (GameObject g in display.slots)
            {
                if (!g) continue;

                // テンプレート以外のスロットを削除
                if (g != display.slotTemplate)
                    Undo.DestroyObjectImmediate(g);
            }
        }

        // テンプレート以外の子オブジェクトをすべて削除
        for (int i = 0; i < display.transform.childCount; i++)
        {
            if (display.transform.GetChild(i).gameObject == display.slotTemplate) continue;
            Undo.DestroyObjectImmediate(display.transform.GetChild(i).gameObject);
            i--; // 子オブジェクトを削除したため、インデックスをデクリメント
        }

        // maxSlotsが0以下なら何もしない
        if (display.maxSlots <= 0) return;

        // 新しいスロット配列を作成
        display.slots = new GameObject[display.maxSlots];
        // 最初のスロットはテンプレート自身
        display.slots[0] = display.slotTemplate;
        // 残りのスロットをテンプレートからインスタンス化して生成
        for (int i = 1; i < display.slots.Length; i++)
        {
            display.slots[i] = Instantiate(display.slotTemplate, display.transform);
            display.slots[i].name = display.slotTemplate.name;
        }
    }
}
