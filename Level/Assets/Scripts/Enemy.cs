using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;
    public float attackRange = 2f;
    public int health = 100;
    
    private Animator animator;
    private NavMeshAgent agent;
    private bool isDead = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            Attack();
        }
        else if (distance > attackRange + 0.5f)
        {
            Chase();
        }
    }

    void Chase()
    {
        bool found = agent.SetDestination(target.position);
        agent.speed = speed;
        if (!found)
        {
            animator.SetBool("walking", false);
        }
        animator.SetBool("walking", true);
        animator.SetBool("attacking", false);
    }

    void Attack()
    {
        agent.speed = 0;
        animator.SetBool("attacking", true);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;
        animator.SetTrigger("hit");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        agent.SetDestination(transform.position);
        animator.SetTrigger("isDead");
        // Optionally, disable the enemy after some time
        Destroy(gameObject, 5f);
    }
}