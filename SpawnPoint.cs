using UnityEngine;
using UnityEngine.UI;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    public int pointID = -1; // Which slot this represents (-1 = auto-assign)
    public int playerID; // Which player owns this spawn point (1 or 2)
    public bool isOccupied = false;
    void Start()
    {
    }

    public void ClearSpawnPoint()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        isOccupied = false;
        //Debug.Log($"🔄 Cleared spawn point {pointID} for Player {playerID}");
    }

}