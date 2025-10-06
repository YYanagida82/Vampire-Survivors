using System;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public List<GameObject> terrainChunks; // 地形チャンクのリスト
    public GameObject player; // プレイヤーオブジェクト
    public float checkerRadius; // チェックする半径
    public LayerMask terrainMask; // 地形のレイヤーマスク
    public GameObject currentChunk; // 現在のチャンク
    Vector3 playerLastPosition; // プレイヤーの最終位置

    [Header("Optimization")]
    public List<GameObject> spawnedChunks; // 生成されたチャンクのリスト
    GameObject lastestChunk; // 最新のチャンク
    public float maxOpDist; // 最適化のための最大距離
    float opDist; // 最適化のための距離
    float optimizerCooldown; // 最適化のクールダウンタイム
    public float optimizerCooldownDur; // クールダウンタイムの持続時間

    void Start()
    {
        playerLastPosition = player.transform.position;
    }

    void Update()
    {
        ChunkChecker();
        ChunkOptimizer();
    }

    void ChunkChecker()
    {
        if (!currentChunk)
        {
            return;
        }

        // プレイヤーからの距離を計算
        Vector3 moveDir = player.transform.position - playerLastPosition;
        playerLastPosition = player.transform.position;

        string directionName = GetDirectionName(moveDir);   // 方向名を取得

        CheckAndSpawnChunk(directionName);  // チャンクを生成

        // 斜め入力時に隣接する部分にもチャンクを生成
        if (directionName.Contains("Up"))
        {
            CheckAndSpawnChunk("Up");
        }
        if (directionName.Contains("Down"))
        {
            CheckAndSpawnChunk("Down");
        }
        if (directionName.Contains("Left"))
        {
            CheckAndSpawnChunk("Left");
        }
        if (directionName.Contains("Right"))
        {
            CheckAndSpawnChunk("Right");
        }
    }

    void CheckAndSpawnChunk(string direction)
    {
        if (!Physics2D.OverlapCircle(currentChunk.transform.Find(direction).position, checkerRadius, terrainMask))
        {
            SpawnChunk(currentChunk.transform.Find(direction).position);
        }
    }

    string GetDirectionName(Vector3 direction)
    {
        direction = direction.normalized;   // 方向ベクトルを正規化

        if (Math.Abs(direction.x) > Math.Abs(direction.y))
        {
            if (direction.y > 0.5f) // 上方向
            {
                return direction.x > 0 ? "Right Up" : "Left Up";    // 右上か左上かを返す
            }
            else if (direction.y < -0.5f)   // 下方向
            {
                return direction.x > 0 ? "Right Down" : "Left Down";    // 右下か左下かを返す
            }
            else
            {
                return direction.x > 0 ? "Right" : "Left";    // 右か左かを返す
            }
        }
        else
        {
            if (direction.x > 0.5f) // 右方向
            {
                return direction.y > 0 ? "Right Up" : "Left Up";    // 右上か左上かを返す
            }
            else if (direction.x < -0.5f)   // 左方向
            {
                return direction.y > 0 ? "Right Down" : "Left Down";    // 右下か左下かを返す
            }
            else
            {
                return direction.y > 0 ? "Up" : "Down";    // 上か下かを返す
            }
        }
    }

    void SpawnChunk(Vector3 spawnPosition)
    {
        // ランダムにチャンクを選んで生成
        int randomIndex = UnityEngine.Random.Range(0, terrainChunks.Count);
        lastestChunk = ObjectPool.instance.Get(terrainChunks[randomIndex], spawnPosition, Quaternion.identity);
        spawnedChunks.Add(lastestChunk);
    }

    void ChunkOptimizer()
    {
        optimizerCooldown -= Time.deltaTime;
        // クールダウンが終わっていなければ処理をスキップ
        if (optimizerCooldown <= 0f)
        {
            optimizerCooldown = optimizerCooldownDur;
        }
        else
        {
            return;
        }

        foreach (GameObject chunk in spawnedChunks)
        {
            // プレイヤーからの距離を計算して、最大距離を超えたら非アクティブにする
            opDist = Vector3.Distance(player.transform.position, chunk.transform.position);
            if (opDist > maxOpDist)
            {
                chunk.SetActive(false);
            }
            else
            {
                chunk.SetActive(true);
            }
        }
    }
}
