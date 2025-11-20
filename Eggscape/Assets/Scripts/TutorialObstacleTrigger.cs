using UnityEngine;

/// <summary>
/// Script que vai no trigger invisível em cima do tronco
/// Detecta quando o player passa por cima e avisa o TutorialManager
/// </summary>
public class TutorialObstacleTrigger : MonoBehaviour
{
    [HideInInspector]
    public TutorialManager tutorialManager;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se é o player e se ainda não foi acionado
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            
            // Avisa o TutorialManager que o player passou
            if (tutorialManager != null)
            {
                tutorialManager.OnPlayerPassedObstacle();
            }
            
            Debug.Log("[TutorialObstacleTrigger] Player detectado passando pelo tronco!");
        }
    }
}