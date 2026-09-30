using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
{
    public int playerID;
    private string enemyTag;
    private bool hasHit = false;
    [SerializeField]
    private AudioSource hitSound1;
    [SerializeField]
    private AudioSource hitSound2;
    private Rigidbody rb;

    void Start()
    {
        enemyTag = playerID == 1 ? "Player2" : "Player1";
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Ground") || enemyTag != null && other.CompareTag(enemyTag))
        {

            //Debug.Log("Arrow hit");
            if (hitSound1 != null && hitSound2 != null && !hitSound1.isPlaying && !hitSound2.isPlaying)
            {
                if (UnityEngine.Random.value > 0.5f)
                {
                    hitSound2.Play();
                }
                else
                {
                    hitSound1.Play();
                }
            }

            hasHit = true;
        }
    }

    void Update()
    {
        if (hasHit)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            Destroy(gameObject, 1f);
        }
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            // Rotate to match velocity, then rotate 90 degrees around X axis
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up) * Quaternion.Euler(0, 90, 0);
        }
    }
}
