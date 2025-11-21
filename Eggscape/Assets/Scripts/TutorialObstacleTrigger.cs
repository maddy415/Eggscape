using UnityEngine;

public class TutorialObstacleTrigger : MonoBehaviour
{
    [HideInInspector] public TutorialManager tutorialManager;
    private bool hasTriggered = false;

    private void Awake()
    {
        if (tutorialManager == null) tutorialManager = FindObjectOfType<TutorialManager>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (hasTriggered) return;

        // procura o Player no parent (robusto contra colliders filhos)
        Player p = other.GetComponentInParent<Player>();
        if (p == null) return;

        hasTriggered = true;
        Debug.Log("[TutorialObstacleTrigger] Player saiu do trigger — chamando OnPlayerTriggeredPass()");

        if (tutorialManager != null)
            tutorialManager.OnPlayerTriggeredPass();
        else
            Debug.LogWarning("[TutorialObstacleTrigger] tutorialManager é nulo.");
    }
}