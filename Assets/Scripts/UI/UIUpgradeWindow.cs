using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// このコンポーネントがアタッチされると、自動的にVerticalLayoutGroupもアタッチされる
[RequireComponent(typeof(VerticalLayoutGroup))]
public class UIUpgradeWindow : MonoBehaviour
{
    VerticalLayoutGroup verticalLayout; // UI要素を縦に整列させるためのコンポーネント

    public RectTransform upgradeOptionTemplate; // アップグレード選択肢のUIテンプレート
    public TextMeshProUGUI tooltipTemplate;     // ツールチップのUIテンプレート

    [Header("Settings")]
    public int maxOptions = 4; // 表示するアップグレード選択肢の最大数
    public string newText = "New!"; // 新規アイテム用の表示テキスト

    // テキストの色設定
    public Color newTextColor = Color.yellow; // 新規アイテムのテキスト色
    public Color levelTextColor = Color.white; // レベルアップアイテムのテキスト色

    [Header("Paths")]
    // UI要素の階層内でのパス
    public string iconPath = "Icon/Item Icon";
    public string namePath = "Name";
    public string descriptionPath = "Description";
    public string buttonPath = "Button";
    public string levelPath = "Level";

    RectTransform rectTransform; // このUI要素のRectTransform
    float optionHeight;          // 各選択肢の高さ
    int activeOptions;           // 現在表示されている選択肢の数

    List<RectTransform> upgradeOptions = new List<RectTransform>(); // 生成されたアップグレード選択肢のリスト

    Vector2 lastScreen; // 前フレームのスクリーンサイズ

    /// <summary>
    /// プレイヤーインベントリとアップグレード候補リストを元に、アップグレード選択肢UIを生成・設定する
    /// </summary>
    /// <param name="inventory">プレイヤーのインベントリ</param>
    /// <param name="possibleUpgrades">アップグレード候補となるアイテムのリスト</param>
    /// <param name="pick">表示する選択肢の数</param>
    /// <param name="tooltip">表示するツールチップのテキスト</param>
    public void SetUpgrades(PlayerInventory inventory, List<ItemData> possibleUpgrades, int pick = 3, string tooltip = "")
    {
        // 表示する選択肢の数を最大数以下に制限
        pick = Mathf.Min(maxOptions, pick);

        // 現在の選択肢オブジェクトが不足している場合、テンプレートから生成して追加
        if (maxOptions > upgradeOptions.Count)
        {
            for (int i = upgradeOptions.Count; i < pick; i++)
            {
                GameObject go = Instantiate(upgradeOptionTemplate.gameObject, transform);
                upgradeOptions.Add((RectTransform)go.transform);
            }
        }

        // ツールチップのテキストを設定し、テキストが空でなければ表示する
        tooltipTemplate.text = tooltip;
        tooltipTemplate.gameObject.SetActive(tooltip.Trim() != "");

        activeOptions = 0;
        int totalPossibleUpgrades = possibleUpgrades.Count;
        // 各選択肢UI要素を設定
        foreach (RectTransform r in upgradeOptions)
        {
            // 表示すべき選択肢が残っているか確認
            if (activeOptions < pick && activeOptions < totalPossibleUpgrades)
            {
                r.gameObject.SetActive(true);

                // 候補リストからランダムにアップグレードを選択し、リストから削除（重複防止）
                ItemData selected = possibleUpgrades[Random.Range(0, possibleUpgrades.Count)];
                possibleUpgrades.Remove(selected);
                Item item = inventory.Get(selected); // プレイヤーがそのアイテムを既に持っているか確認

                // アイテム名を設定
                TextMeshProUGUI name = r.Find(namePath).GetComponent<TextMeshProUGUI>();
                if (name) name.text = selected.GetLevelData(1).name;

                // レベル表示を設定
                TextMeshProUGUI level = r.Find(levelPath).GetComponent<TextMeshProUGUI>();
                if (level)
                {
                    if (item) // アイテムを所持している場合（レベルアップ）
                    {
                        if (item.currentLevel >= item.maxLevel) // 最大レベルの場合
                        {
                            level.text = "Max!";
                            level.color = newTextColor;
                        }
                        else // 次のレベルがある場合
                        {
                            level.text = selected.GetLevelData(item.currentLevel + 1).name;
                            level.color = levelTextColor;
                        }
                    }
                    else // 未所持のアイテムの場合（新規取得）
                    {
                        level.text = newText;
                        level.color = newTextColor;
                    }
                }

                // 説明文を設定
                TextMeshProUGUI desc = r.Find(descriptionPath).GetComponent<TextMeshProUGUI>();
                if (desc)
                {
                    if (item) desc.text = selected.GetLevelData(item.currentLevel + 1).description;
                    else desc.text = selected.GetLevelData(1).description;
                }

                // アイコンを設定
                Image icon = r.Find(iconPath).GetComponent<Image>();
                if (icon) icon.sprite = selected.icon;

                // ボタンのクリックイベントを設定
                Button b = r.Find(buttonPath).GetComponent<Button>();
                if (b)
                {
                    b.onClick.RemoveAllListeners(); // 既存のイベントを全て削除
                    if (item) b.onClick.AddListener(() => inventory.LevelUp(item)); // レベルアップ処理を紐付け
                    else b.onClick.AddListener(() => inventory.Add(selected)); // 新規追加処理を紐付け
                }

                activeOptions++;
            }
            else
            {
                // 不要な選択肢は非表示にする
                r.gameObject.SetActive(false);
            }
        }

        // レイアウトを再計算してUIを整える
        RecalculateLayout();
    }

    /// <summary>
    /// UI全体のレイアウトを再計算する
    /// </summary>
    void RecalculateLayout()
    {
        // 各選択肢の高さを計算
        optionHeight = (rectTransform.rect.height - verticalLayout.padding.top - verticalLayout.padding.bottom - (maxOptions - 1) * verticalLayout.spacing);
        if (activeOptions == maxOptions && tooltipTemplate.gameObject.activeSelf)
            optionHeight /= maxOptions + 1; // ツールチップが表示されている場合はその分も考慮
        else
            optionHeight /= maxOptions;

        // ツールチップが表示されている場合、その高さを設定
        if (tooltipTemplate.gameObject.activeSelf)
        {
            RectTransform tooltipRect = (RectTransform)tooltipTemplate.transform;
            tooltipTemplate.gameObject.SetActive(true);
            tooltipRect.sizeDelta = new Vector2(tooltipRect.sizeDelta.x, optionHeight);
            tooltipTemplate.transform.SetAsLastSibling(); // ツールチップを最下部に配置
        }

        // 表示されている各選択肢の高さを設定
        foreach (RectTransform r in upgradeOptions)
        {
            if (!r.gameObject.activeSelf) continue;
            r.sizeDelta = new Vector2(r.sizeDelta.x, optionHeight);
        }
    }

    // 毎フレーム呼ばれる
    void Update()
    {
        // スクリーンサイズが変更されたらレイアウトを再計算
        if (lastScreen.x != Screen.width || lastScreen.y != Screen.height)
        {
            RecalculateLayout();
            lastScreen = new Vector2(Screen.width, Screen.height);
        }
    }

    // オブジェクトが有効になった最初のフレームで呼ばれる
    void Awake()
    {
        // 必要なコンポーネントを取得
        verticalLayout = GetComponentInChildren<VerticalLayoutGroup>();
        rectTransform = (RectTransform)transform;

        // テンプレートを初期設定
        if (tooltipTemplate) tooltipTemplate.gameObject.SetActive(false);
        if (upgradeOptionTemplate) upgradeOptions.Add(upgradeOptionTemplate);
    }

    // Unityエディタでコンポーネントがリセットされたときに呼ばれる
    void Reset()
    {
        // テンプレートの参照を自動で探して設定
        upgradeOptionTemplate = (RectTransform)transform.Find("Upgrade Option");
        tooltipTemplate = transform.Find("Tooltip").GetComponentInChildren<TextMeshProUGUI>();
    }
}
