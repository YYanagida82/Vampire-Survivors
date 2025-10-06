using UnityEngine;

[CreateAssetMenu(fileName = "MobEventData", menuName = "ScriptableObjects/EventData/Mob")]
public class MobEventData : EventData
{
    [Header("Mob Data")]
    [Range(0f, 360f)] public float possibleAngles = 360f;
    [Min(0)] public float spawnRadius = 2f, spawnDistance = 20f;

    // <param name="player">ターゲットとなるプレイヤー。</param>
    public override bool Activate(PlayerStats player = null)
    {
        if (player)
        {
            // possibleAnglesの範囲内でランダムな角度を決定（ラジアンに変換）
            float randomAngle = Random.Range(0f, possibleAngles) * Mathf.Deg2Rad;

            // GetSpawns()で定義されたすべての敵プレハブに対して処理を行う
            foreach (GameObject o in GetSpawns())
            {
                // プレイヤーの位置を基準に、指定された距離と角度で出現位置を計算
                Vector3 spawnPosition = player.transform.position + new Vector3(
                    (spawnDistance + Random.Range(-spawnRadius, spawnRadius)) * Mathf.Cos(randomAngle),
                    (spawnDistance + Random.Range(-spawnRadius, spawnRadius)) * Mathf.Sin(randomAngle)
                );
                ObjectPool.instance.Get(o, spawnPosition, Quaternion.identity); // 敵を生成
            }
        }

        return false;
    }
}
