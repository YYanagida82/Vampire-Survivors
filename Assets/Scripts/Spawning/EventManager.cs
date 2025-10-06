using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム内の時間経過に基づいたイベントを管理するクラス。
/// 配列`events`をタイムラインとして解釈し、指定時間になったイベントを順次実行します。
/// </summary>
public class EventManager : MonoBehaviour
{
    // 実行するイベントのタイムライン。activeAfterでソートされている必要があります。
    public EventData[] events;
    private int eventIndex = 0; // 次に実行するイベントのインデックス

    // EventManagerのシングルトンインスタンス
    public static EventManager instance;

    // 現在実行中のイベントのランタイムデータを保持する内部クラス。
    [System.Serializable]
    public class Event
    {
        public EventData data;      // イベントの設定データ
        public float duration;      // イベントの残り持続時間
        public float cooldown = 0;  // イベント内の次のアクションまでのクールダウン
    }
    // 現在実行中のイベントのリスト
    List<Event> runningEvents = new List<Event>();

    // シーン内にいる全プレイヤーのステータス情報
    PlayerStats[] allPlayers;

    void Start()
    {
        // シングルトンインスタンスを設定
        instance = this;
        // シーン内の全プレイヤーを検索して保持
        allPlayers = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // --- 新しいタイムライン実行ロジック ---
        // タイムラインに次のイベントがあるか確認
        if (eventIndex < events.Length)
        {
            EventData nextEvent = events[eventIndex];

            // 次のイベントの実行時間になったか確認
            if (GameManager.instance.GetElapsedTime() >= nextEvent.activeAfter)
            {
                // 確率と運を考慮してイベントを発生させるか判定
                if (nextEvent.CheckIfWillHappen(allPlayers[Random.Range(0, allPlayers.Length)]))
                {
                    // 実行中イベントリストに追加
                    runningEvents.Add(new Event { data = nextEvent, duration = nextEvent.duration });
                }

                // タイムラインのインデックスを次に進める
                eventIndex++;
            }
        }

        // --- 実行中イベントの処理ロジック（既存のまま） ---
        List<Event> toRemove = new List<Event>();

        // 現在実行中の各イベントを処理
        foreach (Event e in runningEvents)
        {
            // イベントの持続時間を減らす
            e.duration -= Time.deltaTime;
            if (e.duration <= 0)
            {
                // 持続時間が尽きたら削除リストに追加
                toRemove.Add(e);
                continue;
            }

            // イベント内のアクションのクールダウンを減らす
            e.cooldown -= Time.deltaTime;
            if (e.cooldown <= 0)
            {
                // クールダウンが終了したらイベントのアクションを実行
                e.data.Activate(allPlayers[Random.Range(0, allPlayers.Length)]);
                // 次のアクションまでのクールダウンを再設定
                e.cooldown = e.data.GetSpawnInterval();
            }
        }

        // 終了したイベントをリストから削除
        foreach (Event e in toRemove) runningEvents.Remove(e);
    }

    // For Editor tool
    public void SyncToTime(float timeInSeconds)
    {
        // Reset state
        runningEvents.Clear();
        eventIndex = 0;

        // Loop through all events that should have happened by the new time
        while (eventIndex < events.Length && events[eventIndex].activeAfter <= timeInSeconds)
        {
            EventData nextEvent = events[eventIndex];

            // Check if the event should be active
            if (nextEvent.CheckIfWillHappen(allPlayers[Random.Range(0, allPlayers.Length)]))
            {
                // Calculate how long ago the event should have started
                float timeSinceStart = timeInSeconds - nextEvent.activeAfter;

                // If the event should still be running
                if (timeSinceStart < nextEvent.duration)
                {
                    runningEvents.Add(new Event
                    {
                        data = nextEvent,
                        duration = nextEvent.duration - timeSinceStart, // Set remaining duration
                        cooldown = 0 // Start the action immediately
                    });
                }
            }
            
            eventIndex++;
        }
    }
}