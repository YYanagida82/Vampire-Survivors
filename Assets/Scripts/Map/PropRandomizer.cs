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
        // 各スポーンポイントにランダムなオブジェクトを配置
        foreach (GameObject sp in propSpawnPoints)
        {
            int randomIndex = Random.Range(0, propPrefabs.Count);
            GameObject prop = ObjectPool.instance.Get(propPrefabs[randomIndex], sp.transform.position, Quaternion.identity);
            prop.transform.parent = sp.transform; // スポーンポイントの子オブジェクトに設定
        }
    }
}
