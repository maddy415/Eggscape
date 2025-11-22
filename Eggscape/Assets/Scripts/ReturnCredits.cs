using UnityEngine;

public class ReturnCredits : MonoBehaviour
{
    public GameObject returnText;
    public float showAfterSeconds = 25f; // quantos segundos até o texto aparecer
    
    private float timer = 0f;
    private bool creditsEnded = false;

    private void Start()
    {
        returnText.SetActive(false); // começa invisível
    }

    private void Update()
    {
        if (!creditsEnded)
        {
            timer += Time.deltaTime;

            if (timer >= showAfterSeconds)
            {
                returnText.SetActive(true);
                creditsEnded = true;
            }
        }

        if (creditsEnded && Input.GetKeyDown(KeyCode.Escape))
        {
            MenuManager.instance.LoadSceneByName("main_menu");
        }
    }
}