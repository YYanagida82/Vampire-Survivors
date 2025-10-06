using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Events;

[DisallowMultipleComponent]
[CustomEditor(typeof(UICharacterSelector))]
public class UICharacterSelectorEditor : Editor
{
    UICharacterSelector selector;

    private void OnEnable()
    {
        // インスペクター上でUICharacterSelectorを指し示し、その変数にアクセスできるようにする。
        selector = target as UICharacterSelector;
    }

    public override void OnInspectorGUI()
    {
        // インスペクターにボタンを作成し、クリックされるとトグルテンプレートを生成する。
        base.OnInspectorGUI();
        if (GUILayout.Button("Generate Selectable Characters"))
        {
            CreateTogglesForCharacterData();
        }
    }

    public void CreateTogglesForCharacterData()
    {
        // トグルテンプレートが割り当てられていない場合、処理を中断する。
        if (!selector.toggleTemplate) return;

        // トグルテンプレートの親のすべての子をループ処理し、テンプレート以外のすべてを削除する。
        for (int i = selector.toggleTemplate.transform.parent.childCount - 1; i >= 0; i--)
        {
            Toggle tog = selector.toggleTemplate.transform.parent.GetChild(i).GetComponent<Toggle>();
            if (tog == selector.toggleTemplate) continue;
            Undo.DestroyObjectImmediate(tog.gameObject);    // アンドゥできるようにアクションを記録する。
        }

        // UICharacterSelectorコンポーネントに加えられた変更をアンドゥ可能として記録し、トグルリストをクリアする。
        Undo.RecordObject(selector, "Updates to UICharacterSelector.");
        selector.selectableToggles.Clear();
        CharacterData[] characters = UICharacterSelector.GetAllCharacterDataAssets();

        // プロジェクト内のすべてのキャラクターデータアセットに対して、キャラクターセレクター内にトグルを作成する。
        for (int i = 0; i < characters.Length; i++)
        {
            Toggle tog;
            if (i == 0)
            {
                tog = selector.toggleTemplate;
                Undo.RecordObject(tog, "Modifying the template");
            }
            else
            {
                // 元のテンプレートの親の子として、現在のキャラクターのトグルを作成する。
                tog = Instantiate(selector.toggleTemplate, selector.toggleTemplate.transform.parent);
                Undo.RegisterCreatedObjectUndo(tog.gameObject, "Created a new toggle.");
            }

            // 割り当てるキャラクター名、アイコン、武器アイコンを探す。
            Transform characterName = tog.transform.Find(selector.characterNamePath);
            if (characterName && characterName.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp.text = tog.gameObject.name = characters[i].Name;
            }

            Transform characterIcon = tog.transform.Find(selector.characterIconPath);
            if (characterIcon && characterIcon.TryGetComponent(out Image chrIcon))
            {
                chrIcon.sprite = characters[i].Icon;
            }

            Transform weaponIcon = tog.transform.Find(selector.weaponIconPath);
            if (weaponIcon && weaponIcon.TryGetComponent(out Image wpnIcon))
            {
                wpnIcon.sprite = characters[i].StartingWeapon.icon;
            }

            selector.selectableToggles.Add(tog);

            // すべての選択イベントを削除し、どのキャラクタートグルがクリックされたかをチェックする独自のイベントを追加する。
            for (int j = 0; j < tog.onValueChanged.GetPersistentEventCount(); j++)
            {
                if (tog.onValueChanged.GetPersistentMethodName(j) == "Select")
                {
                    UnityEventTools.RemovePersistentListener(tog.onValueChanged, j);
                }
            }
            UnityEventTools.AddObjectPersistentListener(tog.onValueChanged, selector.Select, characters[i]);
        }

        // 変更が完了したら保存されるように登録する。
        EditorUtility.SetDirty(selector);
    }
}
