using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    private TextMeshProUGUI tmPro;
    private RectTransform rectTransform;

    void Awake()
    {
        tmPro = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();

        if (tmPro == null)
        {
            Debug.LogError("FloatingText: TextMeshProUGUI コンポーネントが見つかりません！");
        }
        if (rectTransform == null)
        {
            Debug.LogError("FloatingText: RectTransform コンポーネントが見つかりません！");
        }
    }

    public void Setup(string text, Camera referenceCamera, float fontSize, TMP_FontAsset fontAsset)
    {
        if (tmPro == null || rectTransform == null) return;

        tmPro.text = text;
        tmPro.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmPro.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmPro.fontSize = fontSize;
        if (fontAsset) tmPro.font = fontAsset;

        // GameManagerで親を設定するため、ここではRectTransformのpositionは設定しない
    }

    public void SetPosition(Vector3 worldPosition, Camera referenceCamera, float yOffset = 0f)
    {
        if (rectTransform == null || referenceCamera == null) return;
        rectTransform.position = referenceCamera.WorldToScreenPoint(worldPosition + new Vector3(0, yOffset));
    }

    public void SetColorAlpha(float alpha)
    {
        if (tmPro == null) return;
        Color currentColor = tmPro.color;
        tmPro.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
    }
}
