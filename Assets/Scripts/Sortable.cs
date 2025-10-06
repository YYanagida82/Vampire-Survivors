using UnityEngine;

/// <summary>
/// 2Dゲームで奥行きを表現するため、
/// オブジェクトのY座標に基づいてSpriteRendererの表示順序(sortingOrder)を自動的に調整するクラス（Yソート）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public abstract class Sortable : MonoBehaviour
{
    // 表示順序を調整する対象のSpriteRenderer。
    SpriteRenderer sorted;

    /// ソート機能を有効/無効にするフラグ
    public bool sortingActive = true;

    // sortingOrderが1段階変わるのに必要なY座標の変化量
    // 値が小さいほどより細かく表示順序が切り替わる
    public float minimumDistance = 0.2f;

    // パフォーマンス向上のため前回のフレームで設定したsortingOrderを保持
    int lastSortOrder = 0;

    // 継承先でオーバーライド可能
    protected virtual void Start()
    {
        sorted = GetComponent<SpriteRenderer>();
    }

    // 全てのUpdate処理が終わった後に呼び出される
    protected virtual void LateUpdate()
    {
        // Y座標に基づいて新しいsortingOrderを計算する。
        // Y座標が大きい（画面上）ほどsortingOrderが小さく（奥）なるようにY座標の符号を反転させる
        // minimumDistanceで割ることで、sortingOrderが変化するY座標の「ステップ」を制御する
        int newSortOrder = (int)(-transform.position.y / minimumDistance);

        // 計算したsortingOrderが前回の値と異なる場合のみ値を更新する
        // これによりオブジェクトが垂直方向に移動していない限り不要な更新処理をスキップできる
        if (lastSortOrder != newSortOrder)
        {
            sorted.sortingOrder = newSortOrder;
            // 次のフレームでの比較のために、最後に設定した値を保存する
            lastSortOrder = newSortOrder;
        }
    }
}