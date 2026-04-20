using System.Collections;
using UnityEngine;

/// <summary>
/// Proyectil disparado por enemigos (Mago, Araña).
/// Se configura desde TaskShootProjectile antes de activarse.
///
/// SETUP DEL PREFAB:
///   - Mesh (bola de fuego, tela...)
///   - SphereCollider con Is Trigger = true
///   - Este script
///
/// slowAmount / slowDuration > 0 → aplica ralentización al jugador al impactar.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [HideInInspector] public float damage       = 10f;
    [HideInInspector] public float speed        = 8f;
    [HideInInspector] public float lifetime     = 5f;

    // Efecto especial (araña): ralentiza al jugador
    [HideInInspector] public float slowAmount   = 0f;   // 0 = sin efecto
    [HideInInspector] public float slowDuration = 0f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) return; // ignorar otros enemigos

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);

            if (slowAmount > 0f)
                ph.StartCoroutine(ApplySlow(other.GetComponent<PlayerController>()));
        }

        Destroy(gameObject);
    }

    private IEnumerator ApplySlow(PlayerController pc)
    {
        if (pc == null) yield break;
        pc.moveSpeed -= slowAmount;
        yield return new WaitForSeconds(slowDuration);
        pc.moveSpeed += slowAmount;
    }
}
