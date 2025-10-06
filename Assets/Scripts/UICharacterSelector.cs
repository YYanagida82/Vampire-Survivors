using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterSelector : MonoBehaviour
{
    public CharacterData defaultCharacter; // デフォルトキャラクター
    public static CharacterData selected;
    public UIStatsDisplay statsUI;

    [Header("Template")]
    public Toggle toggleTemplate;
    public string characterNamePath = "Character Name";
    public string weaponIconPath = "Weapon Icon";
    public string characterIconPath = "Character Icon";
    public List<Toggle> selectableToggles = new List<Toggle>();

    [Header("DescriptionBox")]
    public TextMeshProUGUI characterFullName;
    public TextMeshProUGUI characterDescription;
    public Image selectedCharacterIcon;
    public Image selectedCharacterWeapon;

    void Start()
    {
        if (defaultCharacter) Select(defaultCharacter); // デフォルトキャラクターを選択
    }

    public static CharacterData[] GetAllCharacterDataAssets()
    {
        List<CharacterData> characters = new List<CharacterData>();

        CharacterData[] loaded = Resources.LoadAll<CharacterData>("Scriptable Object/Characters");
        characters.AddRange(loaded);
        
        return characters.ToArray();
    }

    public static CharacterData GetData()
    {
        if (selected) return selected;
        else
        {
            CharacterData[] characters = GetAllCharacterDataAssets();
            if (characters.Length > 0) return characters[Random.Range(0, characters.Length)];
        }
        return null;
    }

    public void Select(CharacterData character)
    {
        // キャラクター選択画面のステータスフィールドを更新
        selected = statsUI.character = character;
        statsUI.UpdateStatFields();

        // キャラクター説明枠の情報を更新
        characterFullName.text = character.FullName;
        characterDescription.text = character.CharacterDescription;
        selectedCharacterIcon.sprite = character.Icon;
        selectedCharacterWeapon.sprite = character.StartingWeapon.icon;
    }
}
