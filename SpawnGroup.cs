using UnityEngine;

public class SpawnGroup : MonoBehaviour
{
    public int playerID;
    // Call this after assigning playerID to ensure correct rotation
    public void SetPlayerID(int id)
    {
        playerID = id;
        if (playerID == 2)
        {
            transform.Rotate(0, 180, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
