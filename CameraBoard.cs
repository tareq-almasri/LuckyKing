using UnityEngine;

public class CameraBoard : MonoBehaviour
{
   
    void Start()
    {
        if (LuckyGameManager.Instance.localPlayerID == 2)
        {
           transform.position += new Vector3(4, 0, 0);
           transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    
}
