using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "ScriptableObjects/WaveData")]
public class WaveData : SpawnData
{
    [Header("Wave Data")]

    [Tooltip("画面上の敵がこの数より少ない場合、最低でもこの数になるまで敵をスポーンさせる")]
    [Min(0)] public int startingCount = 0;

    [Tooltip("このウェーブで出現する敵の総数。この数に達すると条件によってはウェーブが終了する")]
    [Min(1)] public uint totalSpawns = uint.MaxValue;

    // ウェーブの終了条件を定義する列挙型
    [System.Flags] public enum ExitCondition { waveDuration = 1, reachedTotalSpawns = 2 }
    [Tooltip("このウェーブが終了する条件を定義する \n waveDuration：durationで指定した秒数に達するまで待つ \n reachedTotalSpawns：totalSpawnsで指定した敵の数に達するまで待つ")]
    public ExitCondition exitCondition = (ExitCondition)1;

    [Tooltip("次のウェーブに進むには全滅させるのが条件")]
    public bool mustKillAll = false;

    [HideInInspector] public uint spawnCount;   // このウェーブで既に出現した敵の数を記録

    public override GameObject[] GetSpawns(int totalEnemies = 0)
    {
        // 基本の出現数
        int count = Random.Range(spawnPerTick.x, spawnPerTick.y);
        
        // 最低保証数(startingCount)より少ない場合
        if (totalEnemies + count < startingCount)
            count = startingCount - totalEnemies;   // 出現数を調整して最低保証数に到達するようにする

        GameObject[] result = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = possibleSpawnObjects[Random.Range(0, possibleSpawnObjects.Length)];
        }

        return result;
    }
}
