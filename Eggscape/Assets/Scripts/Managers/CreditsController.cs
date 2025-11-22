using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("Scroll Credits (Text that moves up)")]
    public RectTransform creditsTextRect;      // RectTransform do TextMeshProUGUI que contém todo o texto dos créditos
    public float totalScrollDuration = 30f;    // duração total do scroll em segundos (DEFINA AQUI)
    public float extraEndPadding = 400f;       // quanto acima da tela o texto precisa ir para "terminar"
    public bool startOnAwake = true;           // começa automaticamente?

    [Header("Center Messages (after scroll)")]
    [TextArea] public string[] centerMessages; // mensagens para aparecer no centro, em sequência
    public TMP_FontAsset centerFont;           // opcional: fonte para o texto do meio
    public float centerFadeIn = 0.6f;
    public float centerHold = 2f;
    public float centerFadeOut = 0.6f;
    public float timeBetweenCenterMessages = 0.5f;

    [Header("Prefabs & UI")]
    public GameObject centerTextPrefab;        // prefab com TMP_Text + CanvasGroup (veja instruções abaixo)
    public string mainMenuSceneName = "main_menu";

    [Header("Controls")]
    public KeyCode skipKey = KeyCode.Escape;   // tecla para voltar/ignorar
    public bool allowSkipToEnd = true;         // se true, ESC durante scroll pula pra mensagens centrais; se false, sai direto

    // internos
    private bool isScrolling = false;
    private bool centerSequenceStarted = false;
    private float scrollSpeed = 0f;            // calculado automaticamente baseado no totalScrollDuration
    private float scrollTimer = 0f;

    private void Start()
    {
        if (startOnAwake)
        {
            StartCredits();
        }
    }

    public void StartCredits()
    {
        if (creditsTextRect == null)
        {
            Debug.LogError("CreditsManager_TimeBased: creditsTextRect não atribuído!");
            return;
        }

        // posiciona o texto pra começar abaixo da área visível
        RectTransform parentRect = creditsTextRect.parent as RectTransform;
        float screenHeight = parentRect.rect.height;

        // calcula altura do conteúdo (preferredHeight do TMP)
        float contentHeight = GetContentHeight();

        // startY: colocamos o conteúdo abaixo da tela (o topo do conteúdo fica abaixo do bottom da tela)
        float startY = -screenHeight / 2f - contentHeight / 2f - 50f;
        creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, startY);

        // endY: ponto em que consideramos "terminado" (conteúdo passou completamente)
        float endY = screenHeight / 2f + contentHeight / 2f + extraEndPadding;

        // calcula velocidade necessária para que o conteúdo percorra (endY - startY) em 'totalScrollDuration' segundos
        float distance = endY - startY;
        if (totalScrollDuration <= 0.0001f)
        {
            scrollSpeed = 0f;
            Debug.LogWarning("CreditsManager_TimeBased: totalScrollDuration muito pequeno, scrollSpeed setado para 0.");
        }
        else
        {
            scrollSpeed = distance / totalScrollDuration;
        }

        // reseta timers/flags
        scrollTimer = 0f;
        isScrolling = true;
        centerSequenceStarted = false;
    }

    private float GetContentHeight()
    {
        var tmp = creditsTextRect.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            return tmp.preferredHeight;
        }
        return creditsTextRect.rect.height;
    }

    private void Update()
    {
        // lógica de rolagem baseada em tempo
        if (isScrolling)
        {
            // avança timer
            scrollTimer += Time.deltaTime;

            // move o texto proporcional ao scrollSpeed (distância/segundo)
            float newY = creditsTextRect.anchoredPosition.y + scrollSpeed * Time.deltaTime;
            creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, newY);

            // condição de término por tempo
            if (scrollTimer >= totalScrollDuration)
            {
                EndScrollAndStartCenterMessages();
            }
        }

        // input para pular / voltar
        if (Input.GetKeyDown(skipKey))
        {
            if (isScrolling)
            {
                if (allowSkipToEnd)
                {
                    // força terminar (ignora tempo restante)
                    EndScrollAndStartCenterMessages();
                }
                else
                {
                    // sai direto pro menu
                    SceneManager.LoadScene(mainMenuSceneName);
                }
            }
            else if (centerSequenceStarted)
            {
                // se as mensagens centrais já começaram, sai pro menu
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                // fallback
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }

    private void EndScrollAndStartCenterMessages()
    {
        if (!isScrolling) return;
        isScrolling = false;

        // garante que o texto fique na posição final (pra evitar "quebras")
        RectTransform parentRect = creditsTextRect.parent as RectTransform;
        float screenHeight = parentRect.rect.height;
        float contentHeight = GetContentHeight();
        float finalY = screenHeight / 2f + contentHeight / 2f + extraEndPadding;
        creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, finalY);

        // inicia a sequência
        StartCoroutine(PlayCenterMessages());
    }

    private IEnumerator PlayCenterMessages()
    {
        centerSequenceStarted = true;
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < centerMessages.Length; i++)
        {
            string msg = centerMessages[i];
            yield return StartCoroutine(SpawnAndAnimateCenterText(msg));
            yield return new WaitForSeconds(timeBetweenCenterMessages);
        }

        // opcional: depois de todas as mensagens, volta pro menu automaticamente
        // SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator SpawnAndAnimateCenterText(string message)
    {
        if (centerTextPrefab == null)
        {
            // fallback: cria dinamicamente um TMP
            GameObject go = new GameObject("CenterMessageTemp");
            go.transform.SetParent(creditsTextRect.parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 200);
            //rect.anchoredPosition = Vector2.zero;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 40;
            if (centerFont != null) tmp.font = centerFont;

            yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, centerFadeIn));
            yield return new WaitForSeconds(centerHold);
            yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, centerFadeOut));

            Destroy(go);
        }
        else
        {
            GameObject go = Instantiate(centerTextPrefab, creditsTextRect.parent, false);
            go.name = "CenterMessage_" + message.Substring(0, Mathf.Min(12, message.Length)).Replace(" ", "_");
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null) tmp = go.GetComponent<TextMeshProUGUI>();

            if (tmp != null)
            {
                tmp.text = message;
                if (centerFont != null) tmp.font = centerFont;
            }

            yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, centerFadeIn));
            yield return new WaitForSeconds(centerHold);
            yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, centerFadeOut));

            Destroy(go);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (duration <= 0.0001f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
