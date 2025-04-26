using System.Collections;
using UnityEngine;

// Script defining weapon behaviour for player

public class WeaponHit : MonoBehaviour
{
    public int damage = 5;
    public int durability = 100;

    private bool canHit = true;
    private float hitCooldown = 1f;

    void OnTriggerEnter(Collider other)
    {
        if (!canHit) return;

        if (other.CompareTag("Enemy") && PlayerAction.instance.inAttack)
        {
            if (other.attachedRigidbody != null)
            {
                Vector3 direction = other.transform.position - transform.position;
                direction.Normalize();
                other.attachedRigidbody.AddForce(direction * damage, ForceMode.Impulse);
            }

            EnemyAction enemy = other.gameObject.GetComponent<EnemyAction>();
            if (enemy != null)
            {
                enemy.ReceiveHit(damage);
                Debug.Log($"Hit enemy! name: {other.gameObject.name} hp: {enemy.health}");
            }

            StartCoroutine(HitCooldown());
        }
    }

    private IEnumerator HitCooldown()
    {
        canHit = false;
        yield return new WaitForSeconds(hitCooldown);
        canHit = true;
    }
}
