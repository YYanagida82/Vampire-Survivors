using UnityEngine;

public class Pickup : Sortable
{
    protected PlayerStats target;
    protected float speed;
    Vector2 initialPosition;
    float initialOffset;

    // --- 種類を区別する列挙型 ---
    public enum PickupType
    {
        Experience,
        Health,
        Other
    }

    [Header("Pickup Type")]
    public PickupType type = PickupType.Experience;

    [System.Serializable]
    public struct BobbingAnimation
    {
        public float frequency; // 周波数
        public Vector2 direction; // 振動方向
    }
    public BobbingAnimation bobbingAnimation = new BobbingAnimation
    {
        frequency = 2f,
        direction = new Vector2(0, 0.3f)
    };

    [Header("Bonuses")]
    public int experience;  // 経験値
    public int health;  // 体力回復量

    [Header("Sounds")]
    public AudioClip experienceSound;
    public AudioClip healthSound;

    protected override void Start()
    {
        base.Start();
        initialPosition = transform.position;
        initialOffset = Random.Range(0, bobbingAnimation.frequency);
    }

    /// <summary>
    /// プールから再利用された際に再初期化
    /// </summary>
    protected virtual void OnEnable()
    {
        // 再利用時にも初期位置・アニメーション位相を更新
        initialPosition = transform.position;
        initialOffset = Random.Range(0, bobbingAnimation.frequency);
        target = null; // 念のため追従ターゲットをリセット
    }

    protected virtual void Update()
    {
        if (target) // プレイヤーに追従
        {
            Vector2 distance = target.transform.position - transform.position;
            if (distance.sqrMagnitude > speed * speed * Time.deltaTime)
                transform.position += (Vector3)distance.normalized * speed * Time.deltaTime;
            else
            {
                if (ObjectPool.instance)
                {
                    ObjectPool.instance.Return(gameObject);
                }
                else
                {
                    gameObject.SetActive(false); // フォールバック
                }
            }
        }
        else
        {
            // 上下に振動する
            transform.position = initialPosition + bobbingAnimation.direction * Mathf.Sin((Time.time + initialOffset) * bobbingAnimation.frequency);
        }
    }

    public virtual bool Collect(PlayerStats target, float speed)
    {
        if (!this.target)
        {
            this.target = target;
            this.speed = speed;
            return true;
        }
        return false;
    }

    protected virtual void OnDisable()
    {
        if (target == null) return;

        // アイテムの効果を適用
        if (experience != 0)
        {
            if (target != null) target.IncreaseExperience(experience);
            if (experienceSound) AudioSource.PlayClipAtPoint(experienceSound, transform.position);
        }
        if (health != 0)
        {
            if (target != null) target.RestoreHealth(health);
            if (healthSound) AudioSource.PlayClipAtPoint(healthSound, transform.position);
        }

        target = null;
    }
}
