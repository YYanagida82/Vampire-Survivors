using UnityEngine;

public abstract class WeaponEffect : MonoBehaviour
{
    [HideInInspector] public PlayerStats owner;
    [HideInInspector] public Weapon weapon;

    public PlayerStats Owner { get { return owner; } }

    public float GetDamage()
    {
        return weapon.GetDamage();
    }
}
