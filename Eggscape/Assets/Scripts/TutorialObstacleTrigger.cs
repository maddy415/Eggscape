using UnityEngine;

/// <summary>
/// Trigger invisível em cima do tronco.
/// Detecta quando o player passa por cima e avisa o TutorialManager.
/// Melhor usar OnTriggerExit2D para garantir que o player já passou pelo tronco.
/// </summary>
public class TutorialObstacleTrigger : MonoBehaviour
{
    [HideInInspector]
    public TutorialManager tutorialManager;

    private bool hasTriggered = false;

    private void Awake()
    {
        // tenta vincular automaticamente se não estiver setado no Inspector
        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutorialManager>();
        }
    }

    /*private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Log apenas para debug; não marca como "passou" aqui
            Debug.Log("[TutorialObstacleTrigger] Player entrou no trigger (OnTriggerEnter2D).");
        }
    }*/

    // Quando o player SAI do trigger, isso significa que ele já passou por cima do tronco
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (tutorialManager != null)
            {
                tutorialManager.OnPlayerPassedObstacle();
            }
            else
            {
                Debug.LogWarning("[TutorialObstacleTrigger] tutorialManager é nulo ao tentar avisar passagem do player.");
            }

            Debug.Log("[TutorialObstacleTrigger] Player saiu do trigger — avisado TutorialManager.");

            // Opcional: desativa ou destrói o trigger pra não ficar ocupando cena
            // gameObject.SetActive(false);
            // Destroy(gameObject); // escolha uma das duas, se fizer sentido
        }
    }
}