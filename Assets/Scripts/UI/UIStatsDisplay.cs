using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

// プレイヤーのステータスをUIに動的に表示するクラス
public class UIStatsDisplay : MonoBehaviour
{
    public PlayerStats player;
    public CharacterData character;
    TextMeshProUGUI statNames, statValues;
    public bool updateInEditor = false;
    public bool displayCurrentHealth = false;

    void OnEnable()
    {
        UpdateStatFields();
    }

    void OnDrawGizmosSelected()
    {
        if (updateInEditor) UpdateStatFields();
    }
    
    public CharacterData.Stats GetDisplayedStats()
    {
        // Gameシーンではプレイヤーのステータスを、Character Selectシーンではキャラクターのステータスを返す
        if (player) return player.Stats;
        else if (character) return character.stats;
        return new CharacterData.Stats();
    }

    public void UpdateStatFields()
    {
        if (!player && !character) return;
    
        if (!statNames) statNames = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (!statValues) statValues = transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        StringBuilder names = new StringBuilder();
        StringBuilder values = new StringBuilder();

        if (displayCurrentHealth)
        {
            names.AppendLine("現在のHP");
            values.AppendLine(player.CurrentHealth.ToString());
        }

        // CharacterData.Statsクラスが持つ全てのpublicインスタンスフィールドを取得
        FieldInfo[] fields = typeof(CharacterData.Stats).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            names.AppendLine(CharacterData.GetStatDisplayName(field.Name));

            object val = field.GetValue(GetDisplayedStats());
            float fval = val is int ? (int)val : (float)val;

            PropertyAttribute attribute = (PropertyAttribute)PropertyAttribute.GetCustomAttribute(field, typeof(PropertyAttribute));
            if (attribute != null && field.FieldType == typeof(float))
            {
                float percentage = Mathf.Round(fval * 100 - 100);

                if (Mathf.Approximately(percentage, 0))
                {
                    values.Append('-').Append('\n');
                }
                else
                {
                    if (percentage > 0) values.Append('+');
                    values.Append(percentage).Append('%').Append('\n');
                }
            }
            else
            {
                values.Append(fval).Append('\n');
            }
        }

        statNames.text = names.ToString();
        statValues.text = values.ToString();
    }

    void Reset()
    {
        player = FindFirstObjectByType<PlayerStats>();
    }
}
