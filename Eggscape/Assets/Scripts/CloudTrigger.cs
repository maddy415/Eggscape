using UnityEngine;

public class CloudTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        CloudsManager manager = FindAnyObjectByType<CloudsManager>();
        if (manager != null && other.transform.IsChildOf(manager.transform))
        {
            manager.RespawnCloud(other.transform);
        }
    }
}