using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

using Photon.Pun;
public class LuckyDice : MonoBehaviourPun
{
    // Returns true if dice is currently animating (not back to initial position)
    public bool IsAnimating()
    {
        // Only animating if dice has rolled and is not at initial position
        return hasRolled && (Vector3.Distance(transform.position, initialPosition) > 0.01f);
    }

    [Header("Dice Settings")]
    public DiceType diceType;
    public int playerID; // 1 or 2

    [Header("Slide Settings")]
    [SerializeField] private float slideForce = 8f;
    [SerializeField] private float slideDuration = 1.2f;

    [Header("Physics")]
    private Rigidbody rb;
    private bool hasRolled = false;
    private int currentValue = 1;
    private Vector3 initialPosition; // Store the initial position

    [Header("Visual")]
    public Material[] diceMaterials; // Different materials for different dice types

    // Dice face rotations for Unity cube (correct face up)
    private readonly Quaternion[] faceRotations = {
        Quaternion.Euler(90, 0, 0),        // 1 - front face
        Quaternion.Euler(0, 0, 90),       // 2 - right face  
        Quaternion.Euler(0, 0, 0),      // 3 - back face
        Quaternion.Euler(180, 0, 0),      // 4 - left face
        Quaternion.Euler(0, 0, 270),      // 5 - top face
        Quaternion.Euler(-90, 0, 0)        // 6 - bottom face
    };

    // Events
    public System.Action<LuckyDice, int> OnDiceRollComplete;

    public enum DiceType
    {
        White,  // Turn order & Count multiplier
        Red,    // Attack multiplier
        Blue    // Defense multiplier
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Store the initial position set in the scene
        initialPosition = transform.position;
        Debug.Log($"🎲 Dice {playerID} initial position stored: {initialPosition}");

        // Start with no constraints - we'll make it kinematic during rolling anyway
        rb.constraints = RigidbodyConstraints.None;

        // Set dice material based on type
        SetDiceMaterial();
    }

    void SetDiceMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && diceMaterials.Length > (int)diceType)
        {
            renderer.material = diceMaterials[(int)diceType];
        }
    }

    public void RollDice()
    {
        Debug.Log($"[DEBUG] RollDice called for {diceType} dice (Player {playerID})");
        Debug.Log($"[DEBUG] RollDice state: hasRolled={hasRolled}, currentValue={currentValue}, rb={rb != null}");
        if (hasRolled) { Debug.Log($"[DEBUG] Dice {diceType} (Player {playerID}) already rolled, skipping."); return; }

        hasRolled = true;

        int targetValue;
        if (MatchTracker.Instance != null && MatchTracker.Instance.IsPvP)
        {
            // Only local player rolls and sends value
            if (playerID == MatchTracker.Instance.localPlayerID)
            {
                targetValue = Random.Range(1, 7);
                Debug.Log($"🎲 Player {playerID} {diceType} dice rolling... Target: {targetValue}");
                StartCoroutine(RollSequence(targetValue));
                // Send value to remote
                photonView.RPC("SetDiceValueRPC", RpcTarget.Others, playerID, (int)diceType, targetValue);
            }
        }
        else
        {
            targetValue = Random.Range(1, 7);
            Debug.Log($"🎲 Player {playerID} {diceType} dice rolling... Target: {targetValue}");
            StartCoroutine(RollSequence(targetValue));
        }
    }
    [PunRPC]
    public void SetDiceValueRPC(int remotePlayerID, int remoteDiceType, int value)
    {
        // Only apply if not local player's dice
        if (MatchTracker.Instance != null && MatchTracker.Instance.IsPvP)
        {
            if (remotePlayerID != MatchTracker.Instance.localPlayerID && (int)diceType == remoteDiceType)
            {
                Debug.Log($"[PUN] Received dice value for Player {remotePlayerID} {((DiceType)remoteDiceType)}: {value}");
                hasRolled = true;
                StartCoroutine(RollSequence(value));
            }
        }
    }
    private IEnumerator RollSequence(int targetValue)
    {
        // Step 1: Set the dice value and rotation immediately (no constraints)
        rb.constraints = RigidbodyConstraints.None; // Allow free rotation for face setting
        SetDiceValue(targetValue);

        // Step 2: Wait a moment for the rotation to be visible
        yield return new WaitForSeconds(0.3f);

        // Step 3: Apply constraints and slide the dice using kinematic movement
        // Freeze X and Z rotation to prevent rolling during slide, allow Y for visual variety
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Player 1: positive Z direction
        // Player 2: negative Z direction
        float slideDirection = playerID == 1 ? 1f : -1f;

        // Make kinematic to prevent collision issues
        rb.isKinematic = true;

        // Smooth slide using transform interpolation
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0f, 0f, slideForce * slideDirection * 0.2f); // Scale down for smooth movement

        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / slideDuration;

            // Use smooth curve for natural deceleration
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothProgress);
            currentPos.y = 0.777f; // Ensure Y stays at lucky height
            transform.position = currentPos;

            yield return null;
        }

        // Ensure final position
        endPos.y = 0.777f;
        transform.position = endPos;

        // Wait 0.7 seconds after sliding is complete
        yield return new WaitForSeconds(0.7f);

        // Return to initial position
        yield return StartCoroutine(ReturnToInitialPosition());
    }

    private IEnumerator ReturnToInitialPosition()
    {
        Debug.Log($"🎲 Dice {playerID} returning to initial position: {initialPosition}");

        Vector3 startPos = transform.position;
        Vector3 endPos = initialPosition;
        endPos.y = 0.777f; // Maintain lucky height

        float returnDuration = 0.5f; // 0.5 seconds to return
        float elapsedTime = 0f;

        while (elapsedTime < returnDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / returnDuration;

            // Smooth return movement
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothProgress);
            currentPos.y = 0.777f; // Keep at lucky height
            transform.position = currentPos;

            yield return null;
        }

        // Ensure final position
        transform.position = endPos;
        Debug.Log($"✅ Dice {playerID} returned to initial position");
    }

    private void SetDiceValue(int value)
    {
        currentValue = value;

        // Ensure dice is at correct height above board (Lucky number!)
        Vector3 pos = transform.position;
        pos.y = 0.777f;
        transform.position = pos;

        // Set rotation to show correct face (with some Y rotation variety)
        float randomY = Random.Range(0f, 360f);
        Vector3 baseRotation = faceRotations[value - 1].eulerAngles;

        // Debug the rotation being applied
        Debug.Log($"🔍 Setting dice {playerID} to value {value}: applying rotation ({baseRotation.x}, {randomY}, {baseRotation.z})");

        transform.rotation = Quaternion.Euler(baseRotation.x, randomY, baseRotation.z);

        // Additional debug - check what we actually set
        Vector3 actualRotation = transform.rotation.eulerAngles;
        Debug.Log($"🔍 Actual rotation set: ({actualRotation.x}, {actualRotation.y}, {actualRotation.z})");

        Debug.Log($"✅ Player {playerID} {diceType} dice result: {currentValue} (target was: {value})");

        // Notify listeners (only when called from the end of roll sequence)
        OnDiceRollComplete?.Invoke(this, currentValue);
    }

    public void ResetDice()
    {
        Debug.Log($"🔄 Resetting dice {playerID} for re-roll");

        hasRolled = false;
        currentValue = 1;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        // Return to initial position immediately (for re-rolls)
        Vector3 resetPos = initialPosition;
        resetPos.y = 0.777f;
        transform.position = resetPos;
        transform.rotation = Quaternion.identity;

        Debug.Log($"✅ Dice {playerID} reset and ready for re-roll");
    }

    // Properties
    public int Value => currentValue;
    public bool HasRolled => hasRolled;
    public DiceType Type => diceType;
    public int PlayerID => playerID;
}
