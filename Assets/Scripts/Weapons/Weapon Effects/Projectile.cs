using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : WeaponEffect  // 発射武器クラス
{
    public enum DamageSoure { projectille, owner }; // ダメージ元
    public DamageSoure damageSoure = DamageSoure.projectille;
    public bool hasAutoAim = false; // オートエイム持ちか
    public Vector3 rotationSpeed = new Vector3(0, 0, 0); // 回転速度

    protected Rigidbody2D rb;
    protected int piercing; // 貫通数

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void OnEnable()
    {
        if (weapon == null) return;
        Initialise();
    }

    protected virtual void Start()
    {
        Initialise();
    }

    public virtual void Initialise()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource) audioSource.Play();

        Weapon.Stats stats = weapon.GetStats();
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.angularVelocity = rotationSpeed.z;   // 回転速度を設定
            rb.linearVelocity = transform.right * stats.speed * weapon.Owner.Stats.speed;  // 速度を設定
        }

        // 武器の範囲を調整
        float area = weapon.GetArea();
        if (area <= 0) area = 1;
        transform.localScale = new Vector3(
            area * Mathf.Sign(transform.localScale.x),
            area * Mathf.Sign(transform.localScale.y), 1
        );

        piercing = stats.piercing;  //貫通数を保存

        // 一定時間後に非アクティブ化
        if (stats.lifespan > 0)
        {
            // 既存のコルーチンが動いている場合は停止
            StopCoroutine("ReturnToPoolAfterLifetime");
            // 新しい寿命でコルーチンを開始
            StartCoroutine(ReturnToPoolAfterLifetime(stats.lifespan));
        }

        // オートエイム処理実行
            if (hasAutoAim) AcquireAutoAimFacing();
    }

    protected virtual void OnDisable()
    {
        // Invokeが残っていると、非アクティブなオブジェクトに対して呼び出そうとしてエラーになるためキャンセルする
        CancelInvoke();
        ObjectPool.instance.Return(gameObject);
    }

    public virtual void AcquireAutoAimFacing()
    {
        float aimAngle;

        EnemyStats[] targets = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);

        if (targets.Length > 0)
        {
            // 最も近い敵をランダムに選択
            EnemyStats selectedTarget = targets[Random.Range(0, targets.Length)];
            Vector2 difference = selectedTarget.transform.position - transform.position;
            aimAngle = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        }
        else
        {
            aimAngle = Random.Range(0f, 360f);
        }

        transform.rotation = Quaternion.Euler(0, 0, aimAngle);  // 角度に応じて回転
    }

    protected virtual void FixedUpdate()
    {
        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            Weapon.Stats stats = weapon.GetStats();
            transform.position += transform.right * stats.speed * weapon.Owner.Stats.speed * Time.deltaTime;
            rb.MovePosition(transform.position);
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        EnemyStats es = other.GetComponent<EnemyStats>();
        BreakableProps p = other.GetComponent<BreakableProps>();

        // 敵に当たった場合
        if (es)
        {
            Vector3 source = damageSoure == DamageSoure.owner && owner ? owner.transform.position : transform.position;
            es.TakeDamage(GetDamage(), source);

            Weapon.Stats stats = weapon.GetStats();
            weapon.ApplyBuffs(es);  // バフを適用
            piercing--;
            if (stats.hitEffect)
            {
                ObjectPool.instance.Get(stats.hitEffect.gameObject, transform.position, Quaternion.identity);
            }
            if (stats.hitSound)
            {
                // 発射物の位置で一度だけ再生
                AudioSource.PlayClipAtPoint(stats.hitSound, transform.position);
            }
        }
        else if (p)
        {
            p.TakeDamage(GetDamage());
            piercing--;

            Weapon.Stats stats = weapon.GetStats();
            if (stats.hitEffect)
            {
                ObjectPool.instance.Get(stats.hitEffect.gameObject, transform.position, Quaternion.identity);
            }
            // if (stats.hitSound)
            // {
            //     AudioSource.PlayClipAtPoint(stats.hitSound, transform.position);
            // }
        }

        // 貫通数がなくなったら非アクティブ化
        if (piercing <= 0)
        {
            if (ObjectPool.instance)
            {
                ObjectPool.instance.Return(gameObject); // プールに戻す
            }
            else
            {
                gameObject.SetActive(false); // フォールバック
            }
        }
    }
    
    // 寿命が尽きたらオブジェクトをプールに戻すコルーチン
    private System.Collections.IEnumerator ReturnToPoolAfterLifetime(float duration)
    {
        // 指定された時間待機
        yield return new WaitForSeconds(duration);

        // まだアクティブな状態であればプールに戻す
        // (貫通数が尽きていない場合などに備える)
        if (gameObject.activeInHierarchy && ObjectPool.instance)
        {
            ObjectPool.instance.Return(gameObject);
        }
    }
}