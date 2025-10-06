using UnityEditor;
using UnityEngine;

// 時間をスキップするためのカスタムエディタウィンドウ
public class TimeSkipEditor : EditorWindow
{
    // 入力フィールド用の変数
    private int minutes = 0;
    private int seconds = 0;

    // "Tools/Time Skip"メニューからウィンドウを表示するためのメソッド
    [MenuItem("Tools/Time Skip")]
    public static void ShowWindow()
    {
        // 既存のウィンドウインスタンスを取得するか、新しいウィンドウを作成する
        GetWindow<TimeSkipEditor>("Time Skip");
    }

    // ウィンドウのGUIを描画するためのメソッド
    private void OnGUI()
    {
        // ウィンドウのタイトル
        GUILayout.Label("Skip to Time", EditorStyles.boldLabel);
        
        // 分と秒の入力フィールド
        minutes = EditorGUILayout.IntField("Minutes (分)", minutes);
        seconds = EditorGUILayout.IntField("Seconds (秒)", seconds);

        // "Skip Time"ボタン
        if (GUILayout.Button("Skip Time"))
        {
            // エディタが再生モードであるかを確認
            if (Application.isPlaying)
            {
                // 入力された分と秒を合計秒数に変換
                float timeInSeconds = (minutes * 60) + seconds;
                
                // GameManagerのインスタンスを取得
                GameManager gm = GameManager.instance;
                if (gm != null)
                {
                    // GameManagerのSkipToメソッドを呼び出して時間を進める
                    gm.SkipTo(timeInSeconds);
                    Debug.Log($"ゲーム内時間を {minutes:00}:{seconds:00} にスキップしました。");
                }
                else
                {
                    Debug.LogError("GameManagerのインスタンスが見つかりません。ゲームが実行中であることを確認してください。");
                }
            }
            else
            {
                Debug.LogWarning("時間スキップ機能はプレイモード中のみ使用できます。");
            }
        }
    }
}
