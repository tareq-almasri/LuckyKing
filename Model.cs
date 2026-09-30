using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Data;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Animator))]
public class Model : MonoBehaviour, IPointerDownHandler
{
    // Static cache for all enemy models
    private static readonly List<Model> cachedModels = new();
    private static float lastModelCacheTime = -1f;
    private const float modelCacheInterval = 1f; // seconds
    public bool isAlive = true;
    public Card unitCard;
    public UnitStats unitStats;
    public bool isFormation = true;
    public bool isModelCharging = false;
    public bool isAttacking = false;
    public int opponentID = 2;
    private float moveSpeed = 4f; // Adjust as needed
    private float attackRange;
    private int frameCounter = 0;
    private const int framesBetweenUpdates = 1;
    private float initialChargeEnd;
    private float flankChargeEnd;
    private float attackInterval = 0; // Time between projectile launches
    private float lastAttackTime = -1f;
    private Vector3 currentPosition;
    private Vector3 lastPosition;
    private int stuckFrameCount = 0;
    private const int stuckFrameThreshold = 30;
    public int maxHp;
    public int maxArmor;
    public Slider healthBar;
    public Slider armorBar;
    public Image fillImage;
    public Transform hpBarCanvas;
    public GameObject Dust;
    private Quaternion targetRotation;
    public GameObject explosionEffect;
    public GameObject explosionEffect2;
    public GameObject explosionEffect3;
    private int KeepMovingFrameCount = 0;
    private int waitArrow = 0;
    public GameObject muzzleFlashEffect;
    [SerializeField]
    private AudioSource chargeSound1;
    [SerializeField]
    private AudioSource chargeSound2;
    [SerializeField]
    private AudioSource attackSound1;
    [SerializeField]
    private AudioSource attackSound2;
    [SerializeField]
    private AudioSource deathSound;
    private bool hasPlayedDeathSound = false;
    private bool hasPlayedVictorySound = false;
    private float projectileSpeed = 26f; // Default projectile speed
    private float arrowSpeed = 19f; // Default arrow speed
    private float projectileAngleFactor = 9f; // Factor to adjust projectile arc
    public GameObject unitDetailsPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if (isFormation) return;

        if (isFormation)
        {
            healthBar.gameObject.SetActive(false);
            armorBar.gameObject.SetActive(false);
            Dust.SetActive(false);
            return;
        }

        if (unitCard != null) opponentID = unitCard.playerID == 1 ? 2 : 1;

        if (unitCard != null)
        {
            if (unitCard.cardType == CardType.Infantry && unitCard.order1_charge || unitCard.cardType == CardType.Cavalry && unitCard.order2_slowCharge)
            {
                moveSpeed = 6f; // Increase speed for infantry
            }
            if (unitCard.cardType == CardType.Cavalry && !unitCard.order2_slowCharge)
            {
                moveSpeed = 9f; // Increase speed for cavalry
            }
            if (unitCard.cardType == CardType.SiegeEngine)
            {
                moveSpeed = 2f; // Adjust speed for siege engines
            }
        }


        attackRange = unitCard.range;
        UpdateCachedModels();
        initialChargeEnd = Time.time + 8f;
        flankChargeEnd = Time.time + 15f;
        lastPosition = transform.position;
        maxHp = unitStats.hp;
        maxArmor = unitStats.armor;

        if (Dust != null)
        {
            Dust.SetActive(false);
        }

        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.SetActive(false);
        }


        // Change fill color
        if (fillImage != null)
        {
            fillImage.color = unitCard.playerID == 1 ? Color.darkGreen : Color.red;
        }

        if (unitCard.name.Contains("Cannon") || unitCard.name.Contains("Artillery"))
        {
            projectileSpeed = 40f;
            projectileAngleFactor = 0.5f;
        }

        if (unitCard.isArcher)
        {
            projectileAngleFactor = 5f;
        }

        if (unitCard.name.Contains("Crossbow"))
        {
            projectileAngleFactor = 3f;
            arrowSpeed = 25f;
        }

        attackInterval = 4 / unitStats.attackSpeed;

    }
    // Helper to update cached model list
    private void UpdateCachedModels()
    {
        if (Time.time - lastModelCacheTime > modelCacheInterval || cachedModels.Count == 0)
        {
            cachedModels.Clear();
            foreach (var model in FindObjectsByType<Model>(FindObjectsSortMode.None))
            {
                if (model == null) continue;
                cachedModels.Add(model);
            }
            lastModelCacheTime = Time.time;
        }
    }
    // Method to deactivate the explosion effect
    private void DeactivateMuzzleFlash()
    {
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        return;
        // Show details panel
        if (unitDetailsPanel != null)
        {
            unitDetailsPanel.SetActive(true);
            var detailsDisplay = unitDetailsPanel.GetComponent<CardDetailsDisplay>();
            if (detailsDisplay != null && unitCard != null)
            {
                detailsDisplay.ShowDetails(unitCard);
            }
        }
    }
    public void UpdateHealthBar()
    {
        if (healthBar != null && maxHp > 0)
        {
            if ((float)unitStats.hp / maxHp > 0.1 || (float)unitStats.hp / maxHp == 0)
            {
                healthBar.value = (float)unitStats.hp / maxHp;
            }
        }
    }
    public void UpdateArmorBar()
    {
        if (armorBar != null && maxArmor > 0)
        {
            if ((float)unitStats.armor / maxArmor > 0.1 || (float)unitStats.armor / maxArmor == 0)
            {
                armorBar.value = (float)unitStats.armor / maxArmor;
            }
        }
    }
    public void Charge()
    {
        isModelCharging = true;
        isAttacking = false;
        Animator[] animators = GetComponentsInChildren<Animator>();
        foreach (var anim in animators)
        {
            anim.SetTrigger("Charge");
        }
    }

    Model FindNearestEnemy()
    {
        UpdateCachedModels();
        Model nearest = null;
        float minDist = Mathf.Infinity;
        foreach (var model in cachedModels)
        {
            if (model == null || model.unitCard == null || !model.isAlive) continue;
            if (model.unitCard.playerID != unitCard.playerID)
            {
                float dist = Vector3.Distance(transform.position, model.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = model;
                }
            }
        }

        return nearest;
    }

    void Update()
    {
        if (isFormation) return; // Skip update if part of a formation

        if (unitCard.unitStats.armor <= 0)
        {
            armorBar.gameObject.SetActive(false);
        }
        /*frameCounter++;
        if (frameCounter >= framesBetweenUpdates)
        {*/
            UpdateArmorBar();
            UpdateHealthBar();
            frameCounter = 0;
            // bool enemyInRange = IsEnemyWithinRange(attackRange);
            Animator[] animators = GetComponentsInChildren<Animator>();

            Model nearestEnemy = FindNearestEnemy();

            if (Time.time > initialChargeEnd && nearestEnemy != null && isAlive)
            {
                Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
                targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f); // Smoothing factor
                transform.rotation = Quaternion.Euler(0, smoothedRotation.eulerAngles.y, 0); // Clamp X/Z rotation
            }

            bool enemyInRange = nearestEnemy != null && Vector3.Distance(transform.position, nearestEnemy.transform.position) <= attackRange;

            // Movement and rotation now handled in FixedUpdate for smoother physics
            if (isModelCharging && unitCard != null)
            {
                foreach (var anim in animators)
                {
                    if (!anim.GetCurrentAnimatorStateInfo(0).IsName("charge"))
                    {
                        anim.SetTrigger("Charge");
                    }
                }
                lastPosition = transform.position;
                KeepMovingFrameCount++;
                if (Dust != null)
                {
                    Dust.SetActive(true);
                }
                if (enemyInRange && KeepMovingFrameCount > 7)
                {
                    KeepMovingFrameCount = 0;
                    isModelCharging = false;
                    isAttacking = true;
                }
                if (chargeSound1 != null && chargeSound2 != null && !chargeSound1.isPlaying && !chargeSound2.isPlaying)
                {
                    if (Random.value > 0.5f)
                    {
                        chargeSound2.Play();
                    }
                    else
                    {
                        chargeSound1.Play();
                    }
                }
            }
            else if (isAttacking && unitCard != null)
            {
                if (!enemyInRange)
                {
                    // Only set trigger if just switched to charging
                    if (!isModelCharging)
                    {
                        foreach (var anim in animators)
                        {
                            anim.SetTrigger("Charge");
                        }
                    }
                    isAttacking = false;
                    isModelCharging = true;
                }
                else
                {
                    Dust.SetActive(false);

                    //stats calculation and attack logic
                    // Model nearestEnemy = FindNearestEnemy();
                    if (Time.time - lastAttackTime > attackInterval)
                    {
                        lastAttackTime = Time.time;
                        if (attackSound1 != null && attackSound2 != null && !attackSound1.isPlaying && !attackSound2.isPlaying)
                        {
                            if (Random.value > 0.5f)
                            {
                                attackSound1.Play();
                            }
                            else
                            {
                                attackSound2.Play();
                            }
                        }
                        if (explosionEffect != null)
                        {
                            explosionEffect.GetComponent<ParticleSystem>().Play();
                        }
                        if (explosionEffect2 != null)
                        {
                            explosionEffect2.GetComponent<ParticleSystem>().Play();
                        }
                        if (explosionEffect3 != null)
                        {
                            explosionEffect3.GetComponent<ParticleSystem>().Play();
                        }
                        if (muzzleFlashEffect != null)
                        {
                            muzzleFlashEffect.SetActive(true);
                            Invoke(nameof(DeactivateMuzzleFlash), 0.2f); // Deactivate after 0.2 seconds
                        }

                        foreach (var anim in animators)
                        {
                            anim.SetTrigger("Attack");
                        }
                        if (unitCard.cardType == CardType.SiegeEngine)
                        {

                            //find rock spawn
                            Transform spawnPoint = transform.Find("RockSpawn");
                            // Create a projectile instance
                            GameObject projectile = Instantiate(unitCard.projectilePrefab, spawnPoint.position, spawnPoint.rotation);
                            Projectile projectileScript = projectile.GetComponent<Projectile>();
                            projectileScript.card = unitCard;
                            // Calculate direction to the target
                            Vector3 direction = (nearestEnemy.transform.position + Vector3.up * projectileAngleFactor - spawnPoint.position).normalized;

                            Rigidbody rb = projectile.GetComponent<Rigidbody>();
                            rb.linearVelocity = direction * projectileSpeed;

                            // Destroy the projectile after some time to avoid clutter
                            Destroy(projectile, 5f);
                        }
                        else
                        {
                            float crit = unitStats.critRate - nearestEnemy.unitStats.evade;
                            // crit = Mathf.Max(crit, 1); // Ensure crit is at least 1
                            int damage = (int)(unitStats.attack + (unitStats.critDamage * crit));
                            if (unitCard.armorPiercing && nearestEnemy.unitStats.armor > 0)
                                {
                                    damage += 50; //armor-piercing bonus damage
                                }
                            if (unitCard.isArcher)
                            {
                                waitArrow++;
                                if (waitArrow >= 1)
                                {
                                    //spawning arrows
                                    Transform arrowSpawnPoint = transform.Find("ArrowSpawn");
                                    float distance = Vector3.Distance(arrowSpawnPoint.position, nearestEnemy.transform.position);
                                    // projectileAngleFactor = Mathf.Clamp(5 - unitCard.range / distance, 1, 13);
                                    if (unitCard.cardType == CardType.Cavalry && nearestEnemy.unitCard.cardType != CardType.Cavalry)
                                    {
                                        projectileAngleFactor = 4f;
                                    }
                                    float distanceFactor = distance / unitCard.range * projectileAngleFactor;
                                    GameObject arrow = Instantiate(unitCard.projectilePrefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);
                                    arrow.GetComponent<Arrow>().playerID = unitCard.playerID;
                                    // Calculate direction to the target
                                    Vector3 direction = (nearestEnemy.transform.position + distanceFactor * Vector3.up - arrowSpawnPoint.position).normalized;

                                    Rigidbody rb = arrow.GetComponent<Rigidbody>();

                                    rb.linearVelocity = direction * arrowSpeed;
                                    waitArrow = 0;
                                    damage += 50; //armor-piercing bonus damage
                                    nearestEnemy.unitStats.armor -= damage;
                                    if (nearestEnemy.unitStats.armor <= 0)
                                    {
                                        nearestEnemy.unitStats.hp -= damage;
                                    }
                                }
                            }
                            else
                            {

                                nearestEnemy.unitStats.armor -= damage;

                                if (nearestEnemy.unitStats.armor <= 0)
                                {
                                    nearestEnemy.unitStats.hp -= damage;
                                }

                            }
                        }
                    }

                }
            }

            if (unitStats.hp <= 0)
            {
                armorBar.gameObject.SetActive(false);
                fillImage.gameObject.SetActive(false);
                isAlive = false;
                isModelCharging = false;
                isAttacking = false;
                // Play death animation
                foreach (var anim in animators)
                {
                    anim.SetTrigger("Death");
                }
                // Optionally, destroy the model after some time
                Destroy(gameObject, 1.5f); // Adjust delay as needed
            }
            //check for winning
            bool enemyAlive = false;
            bool allAlliesDead = true;
            foreach (var m in cachedModels)
            {
                if (m == null || m.unitCard == null || unitCard == null) continue;
                if (m.unitCard.playerID != unitCard.playerID && m.isAlive)
                {
                    enemyAlive = true;
                }
                if (m.unitCard.playerID == unitCard.playerID && m.isAlive)
                {
                    allAlliesDead = false;
                }
            }
            if (allAlliesDead)
            {
                BattleManager.Instance.winnerID = opponentID;
            }
            if (!enemyAlive)
            {
                Dust.SetActive(false);
                isModelCharging = false;
                isAttacking = false;

                foreach (var anim in animators)
                {
                    anim.SetTrigger("Idle");
                }
                BattleManager.Instance.winnerID = unitCard.playerID;
            }
            //Debug.Log($"hp: {unitStats.hp}");
        //}
    }

    void LateUpdate()
    {
        if (isFormation) return;
        if (hpBarCanvas != null && Camera.main != null)
        {
            hpBarCanvas.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
        }
        if (unitStats.hp <= 0 && deathSound != null && !deathSound.isPlaying && !hasPlayedDeathSound)
        {

            deathSound.Play();
            hasPlayedDeathSound = true;
        }
    }
    void FixedUpdate() //for physics only
    {
        if (isFormation)
        {
            transform.position = new Vector3(transform.position.x, 0.71f, transform.position.z);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            return;
        }

        // Only move/rotate if charging
        if (isModelCharging && unitCard != null)
        {
            currentPosition = transform.position;
            if (Time.time > initialChargeEnd)
            {
                if (Vector3.Distance(currentPosition, lastPosition) < 0.1f)
                {
                    stuckFrameCount++;
                    if (stuckFrameCount >= stuckFrameThreshold)
                    {
                        // Stuck
                        Vector3 randomOffset = new(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.3f, 0.3f));
                        transform.position = Vector3.Lerp(transform.position, transform.position + randomOffset, 0.1f); // 0.1f is the smoothing factor
                        stuckFrameCount = 0;
                    }
                }
                else
                {
                    stuckFrameCount = 0;
                }
            }

            if (unitCard.cardType == CardType.Cavalry && unitCard.order3_flank)
            {
                moveSpeed = 10f; // Flanking speed
                if (Time.time < initialChargeEnd)
                {
                    if (transform.position.x < 0) // Left side
                    {
                        transform.rotation = unitCard.playerID == 1 ? Quaternion.LookRotation(Vector3.forward + Vector3.left) : Quaternion.LookRotation(Vector3.back + Vector3.left);
                    }
                    else // Right side
                    {
                        transform.rotation = unitCard.playerID == 1 ? Quaternion.LookRotation(Vector3.forward + Vector3.right) : Quaternion.LookRotation(Vector3.back + Vector3.right);

                    }
                }
                else if (Time.time < flankChargeEnd)
                {
                    Debug.Log("Initial charge ended");
                    Quaternion targetRotation = unitCard.playerID == 1 ? Quaternion.LookRotation(Vector3.forward) : Quaternion.LookRotation(Vector3.back);
                    Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.01f); // Smoothing factor
                    transform.rotation = Quaternion.Euler(0, smoothedRotation.eulerAngles.y, 0);
                }
            }

            // Move forward smoothly in local space
            Vector3 localForward = transform.forward;
            Vector3 targetPosition = transform.position + moveSpeed * Time.fixedDeltaTime * localForward;
            targetPosition.y = 0.71f; // Clamp Y position
            transform.position = Vector3.Lerp(transform.position, targetPosition, 0.5f); // Smoothing factor
        }

        // Always clamp Y position and rotation for all states
        transform.position = new Vector3(transform.position.x, 0.71f, transform.position.z);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

        lastPosition = transform.position;
    }
}
