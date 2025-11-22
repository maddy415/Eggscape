using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditsController : MonoBehaviour
{
    [Header("Scroll Credits (Text that moves up)")]
    public RectTransform creditsTextRect;      // RectTransform do TextMeshProUGUI que contém todo o texto dos créditos
    public float scrollSpeed = 100f;           // pixels por segundo
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
    public CanvasGroup screenFadeCanvasGroup;  // opcional: para controle de fade da tela inteira (ou deixe null)
    public GameObject centerTextPrefab;        // prefab com TMP_Text + CanvasGroup (veja instruções abaixo)
    public string mainMenuSceneName = "main_menu";

    [Header("Controls")]
    public KeyCode skipKey = KeyCode.Escape;   // tecla para voltar/ignorar
    public bool allowSkipToEnd = true;         // se true, ESC durante scroll pula pra mensagens centrais; se false, sai direto

    // internos
    private bool isScrolling = false;
    private bool centerSequenceStarted = false;
    private Coroutine scrollCoroutine;

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
            Debug.LogError("CreditsManager: creditsTextRect não atribuído!");
            return;
        }

        // start position: garante que comece abaixo da tela
        Vector2 anchored = creditsTextRect.anchoredPosition;
        float screenHeight = ((RectTransform)creditsTextRect.parent).rect.height;
        // posiciona o topo do conteúdo abaixo da tela
        anchored.y = -screenHeight / 2f - GetContentHeight() / 2f - 50f;
        creditsTextRect.anchoredPosition = anchored;

        if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(ScrollCredits());
    }

    private float GetContentHeight()
    {
        // tenta pegar o PreferredHeight do TMP (requer componente TMP_Text no mesmo GameObject do RectTransform)
        var tmp = creditsTextRect.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            return tmp.preferredHeight;
        }
        // fallback para o tamanho do rect
        return creditsTextRect.rect.height;
    }

    private IEnumerator ScrollCredits()
    {
        isScrolling = true;
        centerSequenceStarted = false;

        RectTransform parentRect = creditsTextRect.parent as RectTransform;
        float screenHeight = parentRect.rect.height;

        // calcula a posição final (quando o conteúdo já passou totalmente a parte superior da tela)
        float startY = creditsTextRect.anchoredPosition.y;
        float contentHeight = GetContentHeight();
        float endY = screenHeight / 2f + contentHeight / 2f + extraEndPadding;

        while (isScrolling)
        {
            // movimento frame a frame
            float newY = creditsTextRect.anchoredPosition.y + scrollSpeed * Time.deltaTime;
            creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, newY);

            // se já passou do endY, termina
            if (creditsTextRect.anchoredPosition.y >= endY)
            {
                isScrolling = false;
                break;
            }

            yield return null;
        }

        // garante posição final
        creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, endY);

        // chama sequência de mensagens centrais
        StartCoroutine(PlayCenterMessages());
    }

    private IEnumerator PlayCenterMessages()
    {
        centerSequenceStarted = true;

        // espera 0.2s só pra dar respiro
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < centerMessages.Length; i++)
        {
            string msg = centerMessages[i];
            yield return StartCoroutine(SpawnAndAnimateCenterText(msg));
            yield return new WaitForSeconds(timeBetweenCenterMessages);
        }

        // opcional: se quiser sair automaticamente ao final:
        // SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator SpawnAndAnimateCenterText(string message)
    {
        if (centerTextPrefab == null)
        {
            // fallback: cria dinamicamente um TMP sem prefab
            GameObject go = new GameObject("CenterMessageTemp");
            go.transform.SetParent(creditsTextRect.parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 200);
            rect.anchoredPosition = Vector2.zero;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 40;
            if (centerFont != null) tmp.font = centerFont;

            // anima fade in -> hold -> fade out
            yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, centerFadeIn));
            yield return new WaitForSeconds(centerHold);
            yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, centerFadeOut));

            Destroy(go);
        }
        else
        {
            // usa o prefab (recomendado)
            GameObject go = Instantiate(centerTextPrefab, creditsTextRect.parent, false);
            go.name = "CenterMessage_" + message.Substring(0, Mathf.Min(12, message.Length)).Replace(" ", "_");
            // posiciona no centro
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            // pega CanvasGroup e TMP
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

            // anima
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
            float v = Mathf.Lerp(from, to, t / duration);
            cg.alpha = v;
            yield return null;
        }
        cg.alpha = to;
    }

    private void Update()
    {
        if (Input.GetKeyDown(skipKey))
        {
            if (isScrolling)
            {
                if (allowSkipToEnd)
                {
                    // pular para o fim: para a corrotina e força posição final
                    if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
                    isScrolling = false;

                    RectTransform parentRect = creditsTextRect.parent as RectTransform;
                    float screenHeight = parentRect.rect.height;
                    float contentHeight = GetContentHeight();
                    float endY = screenHeight / 2f + contentHeight / 2f + extraEndPadding;
                    creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, endY);

                    // inicia as mensagens centrais
                    if (!centerSequenceStarted)
                        StartCoroutine(PlayCenterMessages());
                }
                else
                {
                    // sair direto pro menu
                    SceneManager.LoadScene(mainMenuSceneName);
                }
            }
            else if (centerSequenceStarted)
            {
                // sair pro menu quando as mensagens centrais já começaram
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                // fallback
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        // opcional: permitir pular segurando e entrar direto
        // if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape)) { ... }
    }
}
