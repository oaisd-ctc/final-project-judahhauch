using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foe_HP : MonoBehaviour
{
    [SerializeField] private float maxHealth = 4f;

    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }


}
