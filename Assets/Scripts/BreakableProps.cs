using UnityEngine;

public class BreakableProps : MonoBehaviour
{
    public float health;

    public void TakeDamage(float dmg)
    {
        health -= dmg;
        if (health <= 0)
        {
            Kill();
        }
    }

    public void Kill()
    {
        DropRateManager drops = GetComponent<DropRateManager>();
        if (drops) drops.Drop();
        
        ObjectPool.instance.Return(gameObject);
    }
}
