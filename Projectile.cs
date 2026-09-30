using UnityEngine;
using System.Linq;

public class Projectile : MonoBehaviour
{
    public Card card;
    public ParticleSystem hitEffect;
    [SerializeField]
    private AudioSource hitSound;
    [SerializeField]
    private AudioSource smallHitSound;
    private string enemyTag;
    private bool hasHit = false;
    private bool effectISPlaying = false;
    private Rigidbody rb;

    void Start()
    {
        enemyTag = card.playerID == 1 ? "Player2" : "Player1";
        rb = GetComponent<Rigidbody>();
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Projectile hit");
        if (other.CompareTag(enemyTag))
        {
            if (smallHitSound != null)
            {
                smallHitSound.Play();
            }
            if (rb != null)
            {
                rb.useGravity = true;
            }
            Model enemy = other.GetComponent<Model>();
            // Handle collision with other objects
            if (enemy != null && enemy.isAlive)
            {
                enemy.unitStats.armor -= card.unitStats.attack;

                if (enemy.unitStats.armor <= 0)
                {
                    enemy.unitStats.hp -= (int)(card.unitStats.attack + card.unitStats.critDamage * (card.unitStats.critRate - enemy.unitStats.evade));
                }
            }

        }
    }

    void Update()
    {
        if (transform.position.y < 1)
        {
            hasHit = true;
            // Stop the object's movement by disabling its Rigidbody's velocity, if it has one
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
        }

    }
    void LateUpdate()
    {
        if (hasHit && !effectISPlaying)
        {
            if (hitEffect != null && !hitEffect.isPlaying)
            {
                hitEffect.Play();
            }
            if (hitSound != null && !hitSound.isPlaying)
            {
                hitSound.Play();
            }
            effectISPlaying = true;
            Destroy(gameObject, 2.5f); // Delay the destruction to allow effects to play
        }
    }
}
