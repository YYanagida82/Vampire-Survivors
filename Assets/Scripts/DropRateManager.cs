using System.Collections.Generic;
using UnityEngine;

public class DropRateManager : MonoBehaviour
{
    [System.Serializable]
    public class Drops
    {
        public string name;
        public GameObject itemPrefab;
        public float dropRate; // ドロップ率（合計が100になるように設定）
    }

    public bool active = false;
    public List<Drops> drops;

    public void Drop()
    {
        if (!gameObject.scene.isLoaded) return; // シーンがロードされていない場合は処理を中断

        float randomNumber = Random.Range(0f, 100f);    // 0から100のランダムな数を生成
        float cumulativeChance = 0f; // 累積確率カウンター
        Drops selectedDrop = null;

        foreach (Drops rate in drops)
        {
            cumulativeChance += rate.dropRate; // 累積確率を更新

            // 抽選された乱数が累積確率の範囲内に入った場合、そのアイテムを選択
            if (randomNumber < cumulativeChance) 
            {
                selectedDrop = rate;
                break; // アイテムが決定したらすぐにループを終了（効率的）
            }
        }

        // 合計ドロップ率が100であれば、selectedDropは必ずnull以外になる
        if (selectedDrop != null)
        {
            ObjectPool.instance.Get(selectedDrop.itemPrefab, transform.position, Quaternion.identity);
        }
    }   
}
