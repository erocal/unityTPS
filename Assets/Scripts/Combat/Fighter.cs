﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Actor
{
    // 近戰
    Melee,
    // 遠程
    Archer,
    // 殭屍
    Zombie,
    // Mutant
    Mutant,
    // 瘟疫醫生
    Plague,
}

public class Fighter : MonoBehaviour
{
    [Header("角色攻擊類型")]
    [SerializeField] public Actor actorType;
    [Header("攻擊傷害")]
    [SerializeField] float attackDamage = 10f;
    [Header("攻擊距離")]
    [SerializeField] float attackRange = 2f;
    [Header("攻擊時間間隔")]
    [SerializeField] float timeBetweenAttack = 2f;

    [Space(20)]
    [Header("要丟出去的Projectile")]
    [SerializeField] Projectile throwProjectile; 
    [Header("手部座標")]
    [SerializeField] Transform hand;

    //[Header("相機")]
    //[SerializeField] GameObject camera;

    Mover mover;
    Animator animator;
    Health health;
    Health targetHealth;
    AnimatorStateInfo baseLayer;

    float timeSinceLastAttack = Mathf.Infinity;

    // Start is called before the first frame update
    void Start()
    {
        mover = GetComponent<Mover>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        health.onDie += OnDie;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetHealth == null || targetHealth.IsDead()) return;

        if (IsInAttackRange())
        {
            mover.CancelMove();
            AttackBehavior();
        }
        else if (CheckHasAttack() && timeSinceLastAttack > timeBetweenAttack)
        {
            //camera.GetComponent<ThirdPersonCamera>().PreAudio();
            mover.MoveTo(targetHealth.transform.position, 1f);
        }

        UpdateTimer();
    }

    // 檢查攻擊動作是否已經結束
    private bool CheckHasAttack()
    {
        baseLayer = animator.GetCurrentAnimatorStateInfo(0);

        // 如果當前動作等於攻擊
        if (baseLayer.fullPathHash == Animator.StringToHash("Base Layer.Attack"))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// 計時器
    /// </summary>
    void UpdateTimer()
    {
        timeSinceLastAttack += Time.deltaTime;
    }

    private void AttackBehavior()
    {
        transform.LookAt(targetHealth.transform);

        if (timeSinceLastAttack > timeBetweenAttack)
        {
            timeSinceLastAttack = 0;
            TriggerAttack();
        }
    }

    private void TriggerAttack()
    {
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    private void Hit()
    {
        if (targetHealth == null || actorType != Actor.Melee) return;

        if (IsInAttackRange())
        {
            targetHealth.TakeDamage(attackDamage);
        }
    }

    private void ZombieHit()
    {
        if (targetHealth == null || actorType != Actor.Zombie) return;

        if (IsInAttackRange())
        {
            targetHealth.TakeDamage(attackDamage);
        }
    }

    private void Shoot()
    {
        if (targetHealth == null || actorType != Actor.Archer) return;

        if (throwProjectile != null)
        {
            Projectile newProjectile = Instantiate(throwProjectile,hand.position,Quaternion.LookRotation(transform.forward));
            newProjectile.Shoot(gameObject);
        }
    }

    /// <summary>
    /// 是否在搭載此Script的敵人的進戰攻擊範圍內
    /// </summary>
    /// <returns>回傳是否在搭載此Script的敵人的進戰攻擊範圍內</returns>
    private bool IsInAttackRange()
    {
        return Vector3.Distance(transform.position, targetHealth.transform.position) < attackRange;
    }

    /// <summary>
    /// 攻擊目標
    /// </summary>
    /// <param name="target"></param>
    public void Attack(Health target)
    {
        targetHealth = target;
    }

    public void CancelTarget()
    {
        targetHealth = null;
    }

    private void OnDie()
    {
        this.enabled = false;
    }

    // Called by Unity
    // 這是自行繪製visable可視化物件，用來設計怪物追蹤玩家的範圍
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
