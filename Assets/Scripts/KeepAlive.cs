using UnityEngine;

public class KeepAlive : MonoBehaviour
{
    private void Awake()
    {
        // keep network manager persistent while scene changes
        DontDestroyOnLoad(gameObject);
    }
}
