using UnityEngine;

// 敵をウェーブでスポーンさせる処理を管理するクラス
public class SpawnManager : MonoBehaviour
{
    // 現在処理中のウェーブのインデックス
    int currentWaveIndex;
    // 現在のウェーブでスポーンした敵の数
    int currentWaveSpawnCount = 0;

    // 各ウェーブのプロパティを定義するWaveDataスクリプタブルオブジェクトの配列
    public WaveData[] data;
    // スポーン位置の計算に使用されるメインカメラへの参照
    public Camera referenceCamera;

    [Tooltip("この数以上の敵がいる場合、パフォーマンスのためにそれ以上のスポーンを停止")]
    public int maximumEnemyCount = 300;
    // スポーン間隔を制御するためのタイマー
    float spawnTimer;
    // 現在のウェーブが開始してからの経過時間
    float currentWaveDuration = 0f;
    // 呪いによる強化フラグ
    public bool boostedByCurse = true;

    // 他のスクリプトから簡単にアクセスできるようにするためのSpawnManagerの静的インスタンス
    public static SpawnManager instance;

    void Start()
    {
        // 静的インスタンスをこのオブジェクトに設定する
        instance = this;
    }

    void Update()
    {
        // スポーンタイマーをデクリメントする
        spawnTimer -= Time.deltaTime;
        // ウェーブ継続時間タイマーをインクリメントする
        currentWaveDuration += Time.deltaTime;

        // スポーンタイマーがゼロに達した場合
        if (spawnTimer <= 0)
        {
            // 現在のウェーブが終了したかどうかを確認する
            if (HasWaveEnded())
            {
                // 次のウェーブに進む
                currentWaveIndex++;
                // 新しいウェーブのために継続時間とスポーン数をリセットする
                currentWaveDuration = currentWaveSpawnCount = 0;

                // これ以上ウェーブがない場合は、このスクリプトを無効にする
                if (currentWaveIndex >= data.Length) enabled = false;

                // このフレームのUpdateメソッドを終了する
                return;
            }

            // 今すぐ敵をスポーンできない場合
            if (!CanSpawn())
            {
                ActivateCooldown();
                return;
            }

            // この間隔でスポーンする敵を取得する
            GameObject[] spawns = data[currentWaveIndex].GetSpawns(EnemyStats.count);

            // スポーンするプレハブを反復処理する
            foreach (GameObject prefab in spawns)
            {
                // インスタンス化する前にまだスポーンできるか再確認する
                if (!CanSpawn()) continue;

                // Get a new enemy instance from the object pool
                ObjectPool.instance.Get(prefab, GeneratePosition(), Quaternion.identity);
                // 現在のウェーブのスポーン数をインクリメントする
                currentWaveSpawnCount++;
            }

            ActivateCooldown();
        }
    }

    // エディタツール用：指定された秒数にゲームの状態を同期させる
    public void SyncToTime(float timeInSeconds)
    {
        // 同期対象の残り時間を記録する変数
        float timeToAccountFor = timeInSeconds;
        int waveIndex = 0;

        // 指定時間から各ウェーブの継続時間を引いていき、正しいウェーブインデックスを見つける
        for (int i = 0; i < data.Length; i++)
        {
            // 現在のウェーブが最後のウェーブではなく、かつ残り時間が現在のウェーブの継続時間より長い場合
            if (i < data.Length - 1 && data[i].duration < timeToAccountFor)
            {
                // 残り時間から現在のウェーブの継続時間を引く
                timeToAccountFor -= data[i].duration;
                // 次のウェーブへ進む
                waveIndex++;
            }
            else
            {
                // 対象のウェーブが見つかったのでループを抜ける
                break;
            }
        }

        // 指定時間がすべてのウェーブの合計継続時間を超えている場合
        if (waveIndex >= data.Length)
        {
            // スポーンを停止し、処理を終了する
            enabled = false;
            return;
        }

        // 状態を設定する
        // 現在のウェーブインデックスを更新
        currentWaveIndex = waveIndex;
        // 現在のウェーブの経過時間を、最終ウェーブの残り時間に設定
        currentWaveDuration = timeToAccountFor;
        // 新しいウェーブのためにスポーン数をリセット
        currentWaveSpawnCount = 0;
        // スポーンタイマーをリセットし、可能であれば即座にスポーンさせる
        spawnTimer = 0;
    }

    public void ActivateCooldown()
    {
        // 呪いによるブースト値を取得
        float curseBoost = boostedByCurse ? GameManager.GetCumulativeCurse() : 1;
        // 呪いの値が高いほど速く出現する
        spawnTimer += data[currentWaveIndex].GetSpawnInterval() / curseBoost;
    }

    // 新しい敵をスポーンできるかどうかを確認する
    public bool CanSpawn()
    {
        // 画面上の敵の最大数に達した場合はスポーンしない
        if (HasExceededMaxEnemies()) return false;

        // このウェーブの合計スポーン数に達した場合はスポーンしない
        if (instance.currentWaveSpawnCount > instance.data[instance.currentWaveIndex].totalSpawns) return false;

        // このウェーブの継続時間を超えた場合はスポーンしない
        if (instance.currentWaveDuration > instance.data[instance.currentWaveIndex].duration) return false;

        // それ以外の場合はスポーンしてもよい
        return true;
    }

    // アクティブな敵の数が最大許容数を超えたかどうかを確認する
    public static bool HasExceededMaxEnemies()
    {
        // SpawnManagerインスタンスがない場合は確認できない
        if (!instance) return false;
        // 現在の敵の数が最大許容数より多い場合はtrueを返す
        if (EnemyStats.count > instance.maximumEnemyCount) return true;
        // それ以外の場合はfalseを返す
        return false;
    }

    // 現在のウェーブを終了するための条件が満たされているかどうかを確認する
    public bool HasWaveEnded()
    {
        // 現在のウェーブのデータを取得する
        WaveData currentWave = data[currentWaveIndex];

        // ウェーブに継続時間の終了条件があるか確認する
        if ((currentWave.exitCondition & WaveData.ExitCondition.waveDuration) > 0)
            // ある場合は、継続時間に達したか確認する
            if (currentWaveDuration < currentWave.duration) return false;

        // ウェーブに合計スポーン数の終了条件があるか確認する
        if ((currentWave.exitCondition & WaveData.ExitCondition.reachedTotalSpawns) > 0)
            // ある場合は、合計スポーン数に達したか確認する
            if (currentWaveSpawnCount < currentWave.totalSpawns) return false;

        // ウェーブを終了するためにすべての敵を倒す必要があるか確認する
        if (currentWave.mustKillAll && EnemyStats.count > 0)
            return false;

        // すべての条件が満たされていれば、ウェーブは終了
        return true;
    }

    // エディタでスクリプトがアタッチされたとき、または値が変更されたときに呼び出される
    void Reset()
    {
        // メインカメラをreferenceCameraフィールドに自動的に割り当てる
        referenceCamera = Camera.main;
    }

    // カメラのビューのすぐ外側にランダムな位置を生成する
    public static Vector3 GeneratePosition()
    {
        // カメラ参照が設定されていることを確認する
        if (!instance.referenceCamera) instance.referenceCamera = Camera.main;

        // ビューポートの端のランダムな座標を生成する
        float x = Random.Range(0f, 1f), y = Random.Range(0f, 1f);

        // カメラからワールドのZ=0平面までの距離を計算する
        float z = -instance.referenceCamera.transform.position.z;

        // 垂直または水平の端にスポーンするかをランダムに選択する
        switch (Random.Range(0, 2))
        {
            case 0:
            default:
                // 左または右の端にスポーンする
                return instance.referenceCamera.ViewportToWorldPoint(new Vector3(Mathf.Round(x), y, z));
            case 1:
                // 上または下の端にスポーンする
                return instance.referenceCamera.ViewportToWorldPoint(new Vector3(x, Mathf.Round(y), z));
        }
    }

    // 指定されたTransformがカメラのビュー境界内にあるかどうかを確認する
    public static bool IsWithinBoundaries(Transform checkedObject)
    {
        // カメラへの参照を取得する
        Camera c = instance && instance.referenceCamera ? instance.referenceCamera : Camera.main;

        // オブジェクトのワールド位置をビューポート座標に変換する
        Vector2 viewport = c.WorldToViewportPoint(checkedObject.position);
        // オブジェクトが水平境界の外にあるか確認する
        if (viewport.x < 0f || viewport.x > 1f) return false;
        // オブジェクトが垂直境界の外にあるか確認する
        if (viewport.y < 0f || viewport.y > 1f) return false;
        // 両方の内側にあればtrueを返す
        return true;
    }
}