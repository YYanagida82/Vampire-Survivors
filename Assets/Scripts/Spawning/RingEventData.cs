using UnityEngine;

[CreateAssetMenu(fileName = "RingEventData", menuName = "ScriptableObjects/EventData/Ring")]
public class RingEventData : EventData
{
    [Header("Mob Data")]
    public ParticleSystem spawnEffectPrefab;
    [Range(0f, 1f)] public float effectAudioVolume = 1f;
    public Vector2 scale = new Vector2(1, 1);
    [Min(0)] public float spawnRadius = 10f, lifespan = 15f;

    public override bool Activate(PlayerStats player = null)
    {
        if (player)
        {
            GameObject[] spawns = GetSpawns();
            float angleOffset = 2 * Mathf.PI / Mathf.Max(1, spawns.Length);
            float currentAngle = 0;
            foreach (GameObject g in spawns)
            {
                Vector3 spawnPosition = player.transform.position + new Vector3(
                    spawnRadius * Mathf.Cos(currentAngle) * scale.x,
                    spawnRadius * Mathf.Sin(currentAngle) * scale.y
                );

                if (spawnEffectPrefab)
                {
                    GameObject effectObj = ObjectPool.instance.Get(
                        spawnEffectPrefab.gameObject, 
                        spawnPosition, 
                        Quaternion.identity
                    );

                    AudioSource audioSource = effectObj.GetComponent<AudioSource>();
                    if (audioSource)
                    {
                        audioSource.volume = effectAudioVolume;
                    }
                }

                GameObject s = ObjectPool.instance.Get(g, spawnPosition, Quaternion.identity);
                if (!s) continue;

                if (lifespan > 0)
                {
                    // 敵オブジェクト自体にコルーチンを開始させる
                    EnemyMovement enemyMovement = s.GetComponent<EnemyMovement>();
                    if (enemyMovement)
                    {
                        // 敵オブジェクトに、指定時間後にプールに戻るコルーチンを開始させる
                        enemyMovement.StartCoroutine(enemyMovement.ReturnToPoolAfterTime(lifespan));
                    }
                    else
                    {
                        // EnemyMovementがない場合はDestroyする
                        Destroy(s, lifespan);
                    }
                }

                currentAngle += angleOffset;
            }
        }

        return false;
    }
}
