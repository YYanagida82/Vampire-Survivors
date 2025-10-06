using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance { get; private set; }

    // プレハブをキーとして、それぞれの ObjectPool<GameObject> を管理
    private Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    // プールされたインスタンスと、それがどのプレハブから作られたかを紐づける辞書
    // 返却時にどのプールに戻すべきかを判断するために使用
    private Dictionary<GameObject, GameObject> prefabByInstance = new Dictionary<GameObject, GameObject>();


    void Awake()
    {
        // シングルトンパターンの実装
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 指定されたプレハブのプールからオブジェクトを取得する
    /// </summary>
    /// <param name="prefab">プールする元となるプレハブ</param>
    /// <param name="position">オブジェクトの初期位置</param>
    /// <param name="rotation">オブジェクトの初期回転</param>
    /// <returns>取得したオブジェクト</returns>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // プレハブに対応するプールが存在しなければ作成する
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        // ObjectPool<GameObject>からオブジェクトを取得
        GameObject obj = pools[prefab].Get();

        // 取得したオブジェクトの位置と回転を設定し、有効化する
        if (obj != null)
        {
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            prefabByInstance[obj] = prefab;
        }

        return obj;
    }

    /// <summary>
    /// オブジェクトをプールに戻す
    /// </summary>
    /// <param name="obj">プールに戻すオブジェクト</param>
    public void Return(GameObject obj)
    {
        // 既に破棄されているオブジェクトのチェック
        if (obj == null || !obj)
        {
            // 既にUnity側で破棄されているか、nullであれば何もしない
            return;
        }

        // どのプレハブから作られたかを確認
        if (!prefabByInstance.TryGetValue(obj, out GameObject prefab))
        {
            // プール管理外のオブジェクトは普通にDestroy
            Destroy(obj);
            return;
        }

        // 返却する直前に紐付け辞書から削除する
        // これにより、他の場所で同時にReturnが呼ばれても、このオブジェクトは「管理外」と判断されるようになる。
        prefabByInstance.Remove(obj);

        // 対応するプールに戻す
        if (pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            // ObjectPool<GameObject>にオブジェクトを返却
            // この際、OnReleasedが実行され、オブジェクトは自動で非アクティブになる
            pool.Release(obj);
        }
        else
        {
            // 紐付けはあるがプールがない場合は破棄
            Destroy(obj);
        }
    }

    /// <summary>
    /// 指定されたプレハブに対応するプールを作成する
    /// </summary>
    /// <param name="prefab">プールする元となるプレハブ</param>
    private void CreatePool(GameObject prefab)
    {
        // ObjectPool<T> のコンストラクタで、オブジェクトのライフサイクルを定義する
        var newPool = new ObjectPool<GameObject>(
            createFunc: () => OnCreate(prefab), // 新規オブジェクト生成時の処理
            actionOnGet: (obj) =>   // プールから取得時の処理
            {
                // 破棄されたオブジェクトでないかチェックする
                if (obj == null || !obj) return;

                // ParticleSystemがあればPlay()で再生を開始
                if (obj.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
                {
                    ps.Play();
                }
                // その他の取得時処理（必要に応じて）
            },
            actionOnRelease: (obj) =>   // プールへ返却時の処理
            {
                // ParticleSystemがあればStopとClearを行う
                if (obj.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
                {
                    // StopEmittingAndClear: エミッションを停止し、既存のパーティクルを全て削除
                    // true: 子のパーティクルシステムも対象にする
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                // Trail Rendererの軌跡をクリアする
                if (obj.TryGetComponent<TrailRenderer>(out TrailRenderer tr))
                {
                    tr.Clear(); // 過去の軌跡データを完全にリセット
                }

                // TextMeshProUGUIがあればテキストをクリアする
                if (obj.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI tmPro))
                {
                    tmPro.text = string.Empty; // テキストの内容を空にする
                    tmPro.color = Color.white; // 色もデフォルトに戻す（GameManagerで色を変えているため）
                }

                // プールに戻す際、非アクティブにする（必須）
                obj.SetActive(false);
            },
            actionOnDestroy: OnDestroyed,       // オブジェクト破棄時の処理
            collectionCheck: true,              // 既にプールにあるオブジェクトを返却しようとした際にエラーを出すか
            defaultCapacity: 10,                // 初期容量（事前に生成しておくオブジェクトの数）
            maxSize: 3000                       // プールに保持できる最大数
        );
        pools.Add(prefab, newPool);
    }

    // ObjectPool<T> のライフサイクルコールバック

    private GameObject OnCreate(GameObject prefab)
    {
        // 新しいオブジェクトを生成
        GameObject obj = Instantiate(prefab);

        // インスタンスとプレハブの紐付けを記録
        prefabByInstance.Add(obj, prefab);

        // 親オブジェクトをこのプールマネージャーにする
        obj.transform.SetParent(this.transform);

        return obj;
    }

    private void OnDestroyed(GameObject obj)
    {
        // 紐付け辞書からも削除
        prefabByInstance.Remove(obj);

        // Play Modeの終了処理中にエラーが出るのを防ぐ
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
    
    /// <summary>
    /// 現在管理されている（アクティブ + 非アクティブ）全オブジェクトの総数を返します。
    /// </summary>
    public int GetTotalPooledObjectCount()
    {
        int totalCount = 0;
        
        // 全てのプレハブのプールを反復処理
        foreach (var pool in pools.Values)
        {
            // CountAllはCountActive + CountInactiveの合計を返します
            totalCount += pool.CountAll;
        }
        
        return totalCount;
    }

    /// <summary>
    /// 特定のプレハブによって生成されたオブジェクトの総数を返します。
    /// </summary>
    /// <param name="prefab">対象のプレハブ</param>
    /// <returns>総オブジェクト数</returns>
    public int GetTotalCountForPrefab(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var pool))
        {
            return pool.CountAll; // CountActive + CountInactive
        }
        return 0;
    }
}