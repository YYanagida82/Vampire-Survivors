using System.Collections;
using UnityEngine;

public class EnemyMovement : Sortable
{
    [Tooltip("チェックを入れると、元のスプライトが右向きだと見なされます")]
    public bool spriteFacesRight = false; // 元のスプライトが右向きかどうか

    protected EnemyStats stats; // 敵のステータスを管理するコンポーネント
    protected Transform player; // 追跡対象のプレイヤー
    protected Rigidbody2D rb;   // 物理演算用
    protected SpriteRenderer sr; // スプライト表示用

    protected Vector2 KnockbackVelocity;    // ノックバックの速度（吹き飛ぶ勢い）
    protected float knockbackDuration;  // ノックバックの持続時間

    // 敵が画面外に出たときのアクションを定義
    public enum OutOfFrameAction { none, respawnAtEdge, despawn }
    // 敵がフレーム外に出たときに選択されるアクション
    public OutOfFrameAction outOfFrameAction = OutOfFrameAction.respawnAtEdge;

    // 敵のノックバック耐性を定義（持続時間と速度に影響を与える）
    // 例えば以下のような敵のバリエーションを作ることが可能
    // 重い敵: knockbackVarianceをvelocityに設定。吹き飛ばされる勢いは弱まるが、硬直時間は変わらない。
    // すぐに体勢を立て直す敵: knockbackVarianceをdurationに設定。吹き飛ばされる勢いは同じだが、すぐに動けるようになる。
    // 非常に耐性が高い敵: knockbackVarianceをvelocity | duration（両方）に設定。勢いも弱まり、硬直時間も短くなる。
    [System.Flags]
    public enum KnockbackVariance { duration = 1, velocity = 2 }
    public KnockbackVariance knockbackVariance = KnockbackVariance.velocity;


    // 敵がカメラのビューの外でスポーンしたかどうかをチェックするフラグ
    protected bool spawnedOutOfFrame = false;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); // SpriteRendererコンポーネントを取得
        spawnedOutOfFrame = !SpawnManager.IsWithinBoundaries(transform);    // 敵が画面の境界外でスポーンしたかどうかを確認
        stats = GetComponent<EnemyStats>(); // EnemyStatsコンポーネントを取得

        // シーン内のすべてのプレイヤーオブジェクトを検索
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        // ターゲットにするプレイヤーをランダムに選択
        if (allPlayers.Length > 0)
        {
            player = allPlayers[Random.Range(0, allPlayers.Length)].transform;
        }
    }

    protected virtual void Update() 
    {
        HandleSpriteFlip();
    }

    protected void FixedUpdate()
    {
        // オブジェクトが非アクティブならすぐに処理を終了
        if (!gameObject.activeInHierarchy) return;

        // 敵がノックバック中の場合
        if (knockbackDuration > 0)
        {
            // ノックバック速度を適用して移動
            rb.position += KnockbackVelocity * Time.fixedDeltaTime;
            // ノックバックの持続時間を減少させる
            knockbackDuration -= Time.fixedDeltaTime;
        }
        else
        {
            // ノックバック中でない場合は、通常の移動を実行
            Move();
            // 敵が画面外に出た場合の処理
            if (HandleOutOfFrameAction())
            {
                return; // Returnが呼ばれたらFixedUpdateの残りの処理を中断
            }
        }
    }

    // 敵がカメラのビューの外にいるときの振る舞い
    protected virtual bool HandleOutOfFrameAction()
    {
        // 敵が境界の外にいる場合
        if (!SpawnManager.IsWithinBoundaries(transform))
        {
            // 選択されたoutOfFrameActionに基づいてアクションを実行
            switch (outOfFrameAction)
            {
                case OutOfFrameAction.none:
                default:
                    // 何もしない
                    break;
                case OutOfFrameAction.respawnAtEdge:
                    // 敵を画面の端の新しいスポーン位置に移動
                    if (rb)
                    {
                        rb.position = SpawnManager.GeneratePosition();
                    }
                    else
                    {
                        transform.position = SpawnManager.GeneratePosition();
                    }
                    break;
                case OutOfFrameAction.despawn:
                    // 敵が最初にフレームの外でスポーンしなかった場合はそれを破壊
                    if (!spawnedOutOfFrame)
                    {
                        ObjectPool.instance.Return(gameObject);
                        return true;
                    }
                    break;
            }
        }
        else
        {
            // 敵が境界内に戻った場合は、フラグをリセット
            spawnedOutOfFrame = false;
        }

        return false;
    }

    // ノックバック処理
    public virtual void Knockback(Vector2 velocity, float duration)
    {
        // すでにノックバックが進行中の場合は新しいノックバックを適用しない
        if (knockbackDuration > 0) return;

        // ノックバックのタイプがなしの場合は何もしない
        if (knockbackVariance == 0) return;

        // knockbackMultiplier(耐性)をどの程度適用するかの指数
        float pow = 1;
        // ノックバックの変動設定を確認
        bool reducesVelocity = (knockbackVariance & KnockbackVariance.velocity) > 0,
             reducesDuration = (knockbackVariance & KnockbackVariance.duration) > 0;

        // 速度と持続時間の両方が減少する場合、影響を調整
        if (reducesVelocity && reducesDuration) pow = 0.5f;

        // ノックバックの速度と持続時間を計算して適用
        KnockbackVelocity = velocity * (reducesVelocity ? Mathf.Pow(stats.Actual.knockbackMultiplier, pow) : 1);
        knockbackDuration = duration * (reducesDuration ? Mathf.Pow(stats.Actual.knockbackMultiplier, pow) : 1);
    }

    protected virtual void HandleSpriteFlip()
    {
        // プレイヤーの方向に応じてスプライトを反転
        if (player != null && sr != null)
        {
            float directionX = player.transform.position.x - transform.position.x;
            if (directionX != 0)
            {
                // 元のスプライトが右向き(true)の場合、左に移動する時に反転(true) -> sr.flipX = true
                // 元のスプライトが左向き(false)の場合、右に移動する時に反転(true) -> sr.flipX = true
                sr.flipX = directionX > 0 ? !spriteFacesRight : spriteFacesRight;
            }
        }
    }

    public virtual void Move()
    {
        // ターゲットがいなくなった、または無効化された場合は移動しない
        if (player == null || stats == null || !player.gameObject.activeInHierarchy)
        {
            return;
        }

        // Rigidbody2Dを使用して物理的に移動
        if (rb)
        {
            rb.MovePosition(Vector2.MoveTowards(
                rb.position,
                player.transform.position,
                stats.Actual.moveSpeed * Time.fixedDeltaTime)
            );
        }
        else  // Rigidbody2Dがない場合はTransformを直接操作して移動
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.transform.position,
                stats.Actual.moveSpeed * Time.fixedDeltaTime
            );
        }
    }

    // 寿命が尽きたらオブジェクトをプールに戻すコルーチン
    public IEnumerator ReturnToPoolAfterTime(float delay)
    {
        // 指定された時間待機
        yield return new WaitForSeconds(delay);

        // 既に画面外脱出などで返却処理がされていないか確認してから実行
        if (gameObject.activeInHierarchy)
        {
            // ObjectPoolに返却
            ObjectPool.instance.Return(gameObject);
            // ※ ObjectPool.Return()内で obj.SetActive(false) が呼ばれるため、
            //   このコルーチンはこのフレームで終了します。
        }
    }

    /// <summary>
    /// オブジェクトプールからの再利用時、移動関連の状態をリセットします。
    /// EnemyStats.OnEnable() から呼び出されます。
    /// </summary>
    public void ResetMovementState()
    {
        // 1. ノックバックの状態を完全にリセット
        KnockbackVelocity = Vector2.zero; // ノックバックによる速度をクリア
        knockbackDuration = 0f;          // ノックバックの持続時間をクリア

        // 2. Rigidbody2Dの物理的な速度と回転をリセット (最も重要)
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            // Rigidbody2Dの位置を再取得した位置に正確に固定する
            // (MovePositionで移動している場合は不要かもしれませんが、保険として)
            rb.transform.position = rb.position;
        }

        // 3. スプライトの向きをリセット (Move()内で制御されている可能性が高いですが、初期状態に戻す)
        if (sr)
        {
            // プレイヤーのいない初期状態のデフォルトの向きに戻す（例：右向きがデフォルトならfalseに）
            sr.flipX = false;
        }
    }
}