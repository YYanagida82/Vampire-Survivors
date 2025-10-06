using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    WeaponData weaponData;
    string[] weaponSubtypes;
    int selectedWeaponSubtype;

    void OnEnable()
    {
        weaponData = (WeaponData)target;    // 武器データをキャッシュする

        // 武器タイプを取得
        System.Type baseType = typeof(Weapon);
        List<System.Type> subTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => baseType.IsAssignableFrom(p) && p != baseType)
            .ToList();

        // タイプ名をリストに変換してセットする
        List<string> subTypesString = subTypes.Select(t => t.Name).ToList();
        subTypesString.Insert(0, "None");
        weaponSubtypes = subTypesString.ToArray();

        // 選択中のタイプをセットする
        selectedWeaponSubtype = Math.Max(0, Array.IndexOf(weaponSubtypes, weaponData.behaviour));
    }

    public override void OnInspectorGUI()
    {
        // インスペクターでドロップダウンを表示
        selectedWeaponSubtype = EditorGUILayout.Popup("Behaviour", Math.Max(0, selectedWeaponSubtype), weaponSubtypes);

        if (selectedWeaponSubtype > 0)
        {
            weaponData.behaviour = weaponSubtypes[selectedWeaponSubtype].ToString();    // 選択されたタイプをセット
            EditorUtility.SetDirty(weaponData); // 保存するオブジェクトをマークする
            DrawDefaultInspector(); // デフォルトのインスペクターを表示
        }
    }
}
