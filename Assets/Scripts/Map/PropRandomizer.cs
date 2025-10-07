using System.Collections.Generic;
using UnityEngine;

public class PropRandomizer : MonoBehaviour
{
    public List<GameObject> propSpawnPoints; // オブジェクトを配置するポイントのリスト
    public List<GameObject> propPrefabs; // 配置するオブジェクトのプレハブリスト
    void Start()
    {
        SpawnProps();
    }
    
    void SpawnProps()
    {
        // 未設定の場合は何もしない
        if (propPrefabs == null || propPrefabs.Count == 0) return;
        if (propSpawnPoints == null || propSpawnPoints.Count == 0) return;
        if (ObjectPool.instance == null) return;

        // 各スポーンポイントにランダムなオブジェクトを配置
        foreach (GameObject sp in propSpawnPoints)
        {
            if (sp == null) continue;

            int randomIndex = Random.Range(0, propPrefabs.Count);

            GameObject prop = ObjectPool.instance.Get(propPrefabs[randomIndex], sp.transform.position, Quaternion.identity);

            if (prop != null) prop.transform.parent = sp.transform; // スポーンポイントの子オブジェクトに設定
        }
    }
}
