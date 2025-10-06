using UnityEngine;

// ゲームイベントを作成するための基本となる抽象クラス
public abstract class EventData : SpawnData
{
    [Header("Event Data")]
    [Range(0f, 1f)] public float probability = 1f;  // イベントの発生確率
    [Range(0f, 1f)] public float luckFactor = 1f;   // プレイヤーの運ステータスが影響を受ける確率

    [Tooltip("値が指定されている場合、イベントはレベルがこの秒数実行された後にのみ発生します")]
    public float activeAfter = 0;

    public abstract bool Activate(PlayerStats player = null);

    // ゲームの経過時間に基づいてイベントがアクティブ化可能かを確認する
    public bool IsActive()
    {
        if (!GameManager.instance) return false;
        if (GameManager.instance.GetElapsedTime() > activeAfter) return true;
        return false;
    }

    // 確率とプレイヤーの運に基づいてイベントが発生するかどうかを判断する
    public bool CheckIfWillHappen(PlayerStats s)
    {
        // 確率1の場合は常に発生
        if (probability >= 1) return true;

        // 運が高いほどチャンス増加
        if (probability / Mathf.Max(1, (s.Stats.luck * luckFactor)) >= Random.Range(0f, 1f))
            return true;

        return false;
    }
}
