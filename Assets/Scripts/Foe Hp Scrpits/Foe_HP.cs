using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foe_HP : MonoBehaviour
{
    [SerializeField] private int maxHealth = 4;

    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;

    }


    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

    }

}
