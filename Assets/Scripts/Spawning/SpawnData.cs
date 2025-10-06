using UnityEngine;

// 敵の出現パターンを定義するための抽象ScriptableObjectクラス
// このクラスを継承して、具体的な出現データアセットを作成する
public abstract class SpawnData : ScriptableObject
{
    [Tooltip("この出現パターンで生成可能なすべてのゲームオブジェクトのリスト")]
    public GameObject[] possibleSpawnObjects = new GameObject[1];

    [Tooltip("スポーン間隔（秒）。XとYの間のランダムな値が使用される")]
    public Vector2 spawnInterval = new Vector2(2, 3);

    [Tooltip("1回の出現タイミングで何体の敵が出現するか。XとYの間のランダムな値が使用される")]
    public Vector2Int spawnPerTick = new Vector2Int(1, 1);

    [Tooltip("この出現パターンが継続する時間（秒）")]
    [Min(0.1f)] public float duration = 60;

    // 次に出現させるゲームオブジェクトの配列を取得する仮想メソッド
    public virtual GameObject[] GetSpawns(int totalEnemies = 0)
    {
        // spawnPerTickで定義された範囲内で出現させる敵の数をランダムに決定
        int count = Random.Range(spawnPerTick.x, spawnPerTick.y);

        // 結果を格納するためのGameObject配列を準備
        GameObject[] result = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            // possibleSpawnObjectsリストからランダムに敵のプレハブを選び、結果配列に格納
            result[i] = possibleSpawnObjects[Random.Range(0, possibleSpawnObjects.Length)];
        }

        // 出現させる敵プレハブの配列を返す
        return result;
    }

    // 次の出現までの待機時間を取得する仮想メソッド
    public virtual float GetSpawnInterval()
    {
        // spawnIntervalで定義された範囲内で、ランダムな待機時間を返す
        return Random.Range(spawnInterval.x, spawnInterval.y);
    }
}
