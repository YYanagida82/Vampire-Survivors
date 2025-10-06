using UnityEngine;

public class PlayerMovement : Sortable
{
    public const float DEFAULT_MOVESPEED = 5f;

    [HideInInspector]
    public float lastHorizontalVector; // 最後に入力された水平移動
    [HideInInspector]
    public float lastVerticalVector; // 最後に入力された垂直移動
    [HideInInspector]
    public Vector2 moveDir; // 移動方向
    [HideInInspector]
    public Vector2 lastMovedVector; // 最後に移動した方向

    // リファレンス
    Rigidbody2D rb;
    PlayerStats player;

    protected override void Start()
    {
        base.Start();
        player = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        lastMovedVector = new Vector2(1f, 0f); // 初期値は右方向
    }

    void Update()
    {
        InputManagement();
    }

    void FixedUpdate()
    {
        Move();
    }

    void InputManagement()
    {
        if(GameManager.instance.isGameOver) return;

        // 入力受付
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 移動方向
        moveDir = new Vector2(moveX, moveY).normalized;

        // 最後に入力された方向を保存
        if (moveDir.x != 0)
        {
            lastHorizontalVector = moveDir.x;
            lastMovedVector = new Vector2(lastHorizontalVector, 0f);
        }
        if (moveDir.y != 0)
        {
            lastVerticalVector = moveDir.y;
            lastMovedVector = new Vector2(0f, lastVerticalVector);
        }

        // 斜め移動時は両方の方向を保存
        if (moveDir.x != 0 && moveDir.y != 0)
        {
            lastMovedVector = new Vector2(lastHorizontalVector, lastVerticalVector);
        }
    }

    void Move()
    {
        if(GameManager.instance.isGameOver) return;

        // 移動処理
        rb.linearVelocity = moveDir * DEFAULT_MOVESPEED * player.Stats.moveSpeed;
    }
}
