using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerAttack : MonoBehaviour
{
    public Transform attackOrigin;
    public float attackRadius = 1f;
    public LayerMask enemyMask;

    public int attackDMG = 25;

    public float cooldownTime = 0.5f;
    private float cooldownTimer = 0f;


    private void Update()
    {
        if (cooldownTimer <= 0) 
        {
         
            if (InputManager.AttackWasPressed)
            {
                Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyMask);
                foreach (var enemy in enemiesInRange)
                {
                    print(enemy);
                    print(enemy.GetComponent<Foe_HP>());
                    enemy.GetComponent<Foe_HP>().TakeDamage(attackDMG);
                    Debug.Log("attacked");
                }

                cooldownTimer = cooldownTime;
            }

        }
        else
        {
            cooldownTimer -= Time.fixedDeltaTime;
        }
    }


    private void OnDrawGizmos()
    {

         
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);

    }



}
