using UnityEngine;
using UnityEditor;
using System.IO;

public class WaveDataGenerator
{
    // ===== 敵プレハブのパス定義 =====
    // プレハブへのパスをここに設定してください。
    // 例: "Assets/Prefabs/Enemies/Bat.prefab"
    // パスが不明または存在しない場合は、""のままでOKです。生成後に手動で割り当ててください。

    [Header("Wave Enemies")]
    private static readonly string batPrefabPath = "";
    private static readonly string skeletonPrefabPath = "";
    private static readonly string ghostPrefabPath = "";
    private static readonly string werewolfPrefabPath = "";
    private static readonly string giantBatPrefabPath = "";
    private static readonly string mummyPrefabPath = "";

    [Header("Event Enemies")]
    private static readonly string mantisPrefabPath = "";
    private static readonly string bigMantisPrefabPath = "";
    private static readonly string plantTrapPrefabPath = "";
    private static readonly string reaperPrefabPath = "";

    [MenuItem("Tools/Generate Full 30-Min Timeline")]
    public static void GenerateTimeline()
    {
        GenerateWaveAssets();
        GenerateEventAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Timeline Generation", "Successfully generated Wave and Event assets in Assets/Resources/", "OK");
    }

    private static void GenerateWaveAssets()
    {
        string dir = "Assets/Scriptable Object/Wave Data";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // Define wave parameters for different time intervals
        var waveParams = new[]
        {
            // 0-1 min
            new { minute = 1, enemies = new[] { batPrefabPath }, interval = new Vector2(1.5f, 2.5f), perTick = new Vector2Int(1, 2), startCount = 0 },
            // 1-5 min
            new { minute = 5, enemies = new[] { batPrefabPath, skeletonPrefabPath }, interval = new Vector2(1.0f, 2.0f), perTick = new Vector2Int(2, 4), startCount = 10 },
            // 5-10 min
            new { minute = 10, enemies = new[] { batPrefabPath, skeletonPrefabPath, ghostPrefabPath }, interval = new Vector2(0.8f, 1.5f), perTick = new Vector2Int(3, 5), startCount = 20 },
            // 10-15 min
            new { minute = 15, enemies = new[] { skeletonPrefabPath, ghostPrefabPath, werewolfPrefabPath }, interval = new Vector2(0.7f, 1.2f), perTick = new Vector2Int(4, 6), startCount = 30 },
            // 15-20 min
            new { minute = 20, enemies = new[] { werewolfPrefabPath, giantBatPrefabPath, mummyPrefabPath }, interval = new Vector2(0.5f, 1.0f), perTick = new Vector2Int(5, 8), startCount = 50 },
            // 20-29 min
            new { minute = 29, enemies = new[] { skeletonPrefabPath, ghostPrefabPath, werewolfPrefabPath, giantBatPrefabPath, mummyPrefabPath }, interval = new Vector2(0.3f, 0.8f), perTick = new Vector2Int(8, 12), startCount = 60 },
            // 29-30 min
            new { minute = 30, enemies = new[] { giantBatPrefabPath, mummyPrefabPath }, interval = new Vector2(0.2f, 0.5f), perTick = new Vector2Int(10, 15), startCount = 80 }
        };

        var currentParams = waveParams[0];
        int paramIndex = 0;

        for (int i = 0; i < 30; i++)
        {
            // Update parameters if the current minute exceeds the defined threshold
            if (i >= waveParams[paramIndex].minute && paramIndex < waveParams.Length - 1)
            {
                paramIndex++;
                currentParams = waveParams[paramIndex];
            }

            WaveData wave = ScriptableObject.CreateInstance<WaveData>();
            wave.duration = 60f; // 1 minute duration for each wave
            wave.exitCondition = WaveData.ExitCondition.waveDuration;

            // Collect enemy prefabs
            var enemyPrefabs = new System.Collections.Generic.List<GameObject>();
            foreach (var path in currentParams.enemies)
            {
                var prefab = GetPrefab(path);
                if (prefab != null)
                {
                    enemyPrefabs.Add(prefab);
                }
            }
            wave.possibleSpawnObjects = enemyPrefabs.ToArray();

            wave.spawnInterval = currentParams.interval;
            wave.spawnPerTick = currentParams.perTick;
            wave.startingCount = currentParams.startCount;

            string waveName = $"Wave_{i:D2}-{i + 1:D2}.asset";
            AssetDatabase.CreateAsset(wave, Path.Combine(dir, waveName));
        }
    }

    private static void GenerateEventAssets()
    {
        string dir = "Assets/Scriptable Object/Event Data";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 5分: カマキリボス
        MobEventData mantisBoss = ScriptableObject.CreateInstance<MobEventData>();
        mantisBoss.activeAfter = 300; // 5:00
        mantisBoss.possibleSpawnObjects = new GameObject[] { GetPrefab(mantisPrefabPath) };
        mantisBoss.spawnPerTick = new Vector2Int(1, 1);
        mantisBoss.duration = 10f; // 10秒間、このイベントがアクティブ
        AssetDatabase.CreateAsset(mantisBoss, Path.Combine(dir, "Event_05_MantisBoss.asset"));

        // 10分: 巨大カマキリ軍団
        MobEventData bigMantisSwarm10 = ScriptableObject.CreateInstance<MobEventData>();
        bigMantisSwarm10.activeAfter = 600; // 10:00
        bigMantisSwarm10.possibleSpawnObjects = new GameObject[] { GetPrefab(bigMantisPrefabPath) };
        bigMantisSwarm10.spawnPerTick = new Vector2Int(3, 5);
        bigMantisSwarm10.duration = 15f;
        AssetDatabase.CreateAsset(bigMantisSwarm10, Path.Combine(dir, "Event_10_BigMantisSwarm.asset"));

        // 10分: 植物トラップリング
        RingEventData plantRing10 = ScriptableObject.CreateInstance<RingEventData>();
        plantRing10.activeAfter = 600; // 10:00
        plantRing10.possibleSpawnObjects = new GameObject[] { GetPrefab(plantTrapPrefabPath) };
        plantRing10.spawnPerTick = new Vector2Int(12, 12); // 12個のトラップを配置
        plantRing10.spawnRadius = 12f;
        plantRing10.lifespan = 20f; // トラップは20秒で消える
        plantRing10.duration = 1f; // 1回実行するだけ
        plantRing10.spawnInterval = new Vector2(1,1); // 1回実行するだけ
        AssetDatabase.CreateAsset(plantRing10, Path.Combine(dir, "Event_10_PlantRing.asset"));

        // 15分: 巨大カマキリ軍団
        MobEventData bigMantisSwarm15 = ScriptableObject.CreateInstance<MobEventData>();
        bigMantisSwarm15.activeAfter = 900; // 15:00
        bigMantisSwarm15.possibleSpawnObjects = new GameObject[] { GetPrefab(bigMantisPrefabPath) };
        bigMantisSwarm15.spawnPerTick = new Vector2Int(5, 8);
        bigMantisSwarm15.duration = 15f;
        AssetDatabase.CreateAsset(bigMantisSwarm15, Path.Combine(dir, "Event_15_BigMantisSwarm.asset"));

        // 15分: 植物トラップリング
        RingEventData plantRing15 = ScriptableObject.CreateInstance<RingEventData>();
        plantRing15.activeAfter = 900; // 15:00
        plantRing15.possibleSpawnObjects = new GameObject[] { GetPrefab(plantTrapPrefabPath) };
        plantRing15.spawnPerTick = new Vector2Int(16, 16);
        plantRing15.spawnRadius = 15f;
        plantRing15.lifespan = 20f;
        plantRing15.duration = 1f;
        plantRing15.spawnInterval = new Vector2(1,1);
        AssetDatabase.CreateAsset(plantRing15, Path.Combine(dir, "Event_15_PlantRing.asset"));

        // 30分: 死神
        MobEventData reaper = ScriptableObject.CreateInstance<MobEventData>();
        reaper.activeAfter = 1800; // 30:00
        reaper.possibleSpawnObjects = new GameObject[] { GetPrefab(reaperPrefabPath) };
        reaper.spawnPerTick = new Vector2Int(1, 1);
        reaper.duration = float.MaxValue; // 出続ける
        reaper.spawnInterval = new Vector2(60, 60); // 60秒ごとに追加
        AssetDatabase.CreateAsset(reaper, Path.Combine(dir, "Event_30_Reaper.asset"));
    }

    private static GameObject GetPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
