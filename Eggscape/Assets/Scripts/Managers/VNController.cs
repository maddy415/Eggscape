using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class VNController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI textTMP;
    [SerializeField] private CanvasGroup clickBlocker; // opcional (pra bloquear clique durante transições)

    [Header("Typing")]
    [Tooltip("Caracteres por segundo (global).")]
    [SerializeField] private float typeSpeed = 45f;
    [Tooltip("Multiplicador de pausa em pontuações (. , ? ! : ;)")]
    [SerializeField] private float punctMultiplier = 6f;
    [Tooltip("Sons por caractere (opcional)")]
    [SerializeField] private AudioSource typeAudio;
    [SerializeField] private AudioClip typeClip;

    [Header("Fundo (opcional)")]
    [Tooltip("Fadear troca de fundo")]
    [SerializeField] private bool fadeBackground = true;
    [SerializeField] private float bgFadeTime = 0.25f;

    [Header("Eventos")]
    public UnityEvent onSequenceEnd; // plugue aqui: carregar próxima cena, voltar ao menu etc.

    private int idx = -1;
    private bool isTyping = false;
    private bool canAdvance = true;
    private Coroutine typingCo;
    private string currentFullText = "";

    // cache pra fade
    private CanvasGroup bgGroup;

    void Awake()
    {
        if (backgroundImage != null)
        {
            bgGroup = backgroundImage.GetComponent<CanvasGroup>();
            if (bgGroup == null) bgGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();
            bgGroup.alpha = 1f;
        }
        if (clickBlocker != null) clickBlocker.alpha = 0f; // não bloqueia por padrão
    }

    void Start()
    {
        textTMP.text = "";
        NextEntry(); // começa a sequência
    }

    void Update()
    {
        // Clique esquerdo ou Space/Enter pra avançar
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnAdvanceInput();
        }
    }

    /// <summary>
    /// Chamado por botões/touch para avançar o diálogo.
    /// </summary>
    public void AdvanceFromUI()
    {
        OnAdvanceInput();
    }

    private void OnAdvanceInput()
    {
        if (!canAdvance) return;

        if (isTyping)
        {
            // pular digitação: mostrar tudo de uma vez
            SkipTypingToEnd();
        }
        else
        {
            // já terminou o texto atual? vai pro próximo
            NextEntry();
        }
    }

    private void NextEntry()
    {
        idx++;
        if (sequence == null || sequence.entries == null || idx >= sequence.entries.Count)
        {
            // fim da sequência
            textTMP.text = "";
            onSequenceEnd?.Invoke();
            return;
        }

        var e = sequence.entries[idx];

        // troca de fundo se configurado
        if (e.backgroundOverride != null)
            StartCoroutine(SetBackground(e.backgroundOverride));
        // inicia digitação desta fala
        StartTyping(e);
    }

    private void StartTyping(DialogueSequence.Entry e)
    {
        if (typingCo != null) StopCoroutine(typingCo);
        currentFullText = e.text ?? "";
        textTMP.text = "";
        typingCo = StartCoroutine(TypeRoutine(e));
    }

    private IEnumerator TypeRoutine(DialogueSequence.Entry e)
    {
        isTyping = true;
        canAdvance = true;

        float cps = (e.localTypeSpeed > 0f) ? e.localTypeSpeed : typeSpeed;
        cps = Mathf.Max(1f, cps);

        float baseDelay = 1f / cps;

        for (int i = 0; i < currentFullText.Length; i++)
        {
            textTMP.text = currentFullText.Substring(0, i + 1);

            // som opcional por caractere
            if (typeAudio != null && typeClip != null)
            {
                // toca com leve variação
                typeAudio.pitch = Random.Range(0.96f, 1.04f);
                typeAudio.PlayOneShot(typeClip, 0.7f);
            }

            char c = currentFullText[i];
            float delay = baseDelay;

            // pausas extras em pontuação
            if (c == '.' || c == ',' || c == '?' || c == '!' || c == ':' || c == ';')
                delay *= punctMultiplier;

            float t = 0f;
            while (t < delay)
            {
                // se o jogador clicar durante a digitação, o Update chamará SkipTypingToEnd()
                // e isTyping vira false, então só sair do laço rápido aqui
                if (!isTyping) break; 
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!isTyping) break;
        }

        // finaliza exibindo tudo
        textTMP.text = currentFullText;
        isTyping = false;

        // segurinha no final, se quiser
        if (e.endHold > 0f)
        {
            canAdvance = false;
            yield return new WaitForSecondsRealtime(e.endHold);
            canAdvance = true;
        }
    }

    private void SkipTypingToEnd()
    {
        if (!isTyping) return;

        isTyping = false;
        if (typingCo != null) StopCoroutine(typingCo);
        textTMP.text = currentFullText;
    }

    private IEnumerator SetBackground(Sprite newBg)
    {
        if (backgroundImage == null) yield break;

        if (fadeBackground && bgGroup != null)
        {
            // opcional: bloquear clique durante a transição
            if (clickBlocker != null) clickBlocker.alpha = 1f;

            // fade out
            float t = 0f;
            while (t < bgFadeTime)
            {
                t += Time.unscaledDeltaTime;
                bgGroup.alpha = Mathf.Lerp(1f, 0f, t / bgFadeTime);
                yield return null;
            }
            bgGroup.alpha = 0f;

            // troca sprite
            backgroundImage.sprite = newBg;
            backgroundImage.SetNativeSize(); // se vc quer manter tamanho nativo; remova se quiser stretch

            // fade in
            t = 0f;
            while (t < bgFadeTime)
            {
                t += Time.unscaledDeltaTime;
                bgGroup.alpha = Mathf.Lerp(0f, 1f, t / bgFadeTime);
                yield return null;
            }
            bgGroup.alpha = 1f;

            if (clickBlocker != null) clickBlocker.alpha = 0f;
        }
        else
        {
            backgroundImage.sprite = newBg;
        }
    }
}
