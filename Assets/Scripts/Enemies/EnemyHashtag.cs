using System.Collections.Generic;
using UnityEngine;

/// HASHTAG: antena pasiva que buffea a los enemigos dentro de su radio
/// (más vida y luz naranja). No ataca, no persigue.
/// Al morir, quita el buff a todos.
[RequireComponent(typeof(SphereCollider))]
public class EnemyHashtag : EnemyBase
{
    [Header("Buff")]
    [Tooltip("Radio del buff")]
    [SerializeField] private float radioBuff = 8f;

    private readonly List<EnemyBase> enemigosBuffeados = new List<EnemyBase>();

    protected override void Start()
    {
        base.Start();
        mataAlTocar = false;

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true;
            sc.radius = radioBuff;
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (muerto) return;
        EnemyBase enemigo = other.GetComponent<EnemyBase>();
        if (enemigo != null && enemigo != this && !enemigosBuffeados.Contains(enemigo))
        {
            enemigosBuffeados.Add(enemigo);
            enemigo.AplicarBuff(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (muerto) return;
        EnemyBase enemigo = other.GetComponent<EnemyBase>();
        if (enemigo != null && enemigosBuffeados.Contains(enemigo))
        {
            enemigosBuffeados.Remove(enemigo);
            enemigo.AplicarBuff(false);
        }
    }

    protected override void OnMuerte()
    {
        foreach (EnemyBase e in enemigosBuffeados)
        {
            if (e != null)
                e.AplicarBuff(false);
        }
        enemigosBuffeados.Clear();
    }
}