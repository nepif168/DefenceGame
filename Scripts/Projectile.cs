using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ProType
{
    cannonBall,
    fireBall
};

public class Projectile : MonoBehaviour {

    [SerializeField]
    int attackStrength;

    [SerializeField]
    ProType projectileType;

    [SerializeField]
    float speed = 1f;

    public int AttackLength
    {
        get
        {
            return attackStrength;
        }
    }

    public ProType ProjectileType
    {
        get
        {
            return ProjectileType;
        }
    }

    public float Speed
    {
        get { return speed; }
    }
}
