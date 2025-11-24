using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsController : MonoBehaviour
{
    [Header("Scroll Credits (Text that moves up)")]
    public RectTransform creditsTextRect;      
    public float totalScrollDuration = 30f;    
    public float extraEndPadding = 400f;       
    public bool startOnAwake = true;           

    [Header("Center Messages (after scroll)")]
    [TextArea] public string[] centerMessages; 
    public TMP_FontAsset centerFont;           
    public float centerFadeIn = 0.6f;
    public float centerHold = 2f;
    public float centerFadeOut = 0.6f;
    public float timeBetweenCenterMessages = 0.5f;

    [Header("Prefabs & UI")]
    public GameObject centerTextPrefab;        
    public string mainMenuSceneName = "main_menu";

    [Header("Controls")]
    public KeyCode skipKey = KeyCode.Escape;   
    public bool allowSkipToEnd = true;         

    private bool isScrolling = false;
    private bool centerSequenceStarted = false;
    private float scrollSpeed = 0f;            
    private float scrollTimer = 0f;

    private void Start()
    {
        if (startOnAwake)
            StartCredits();
    }

    public void StartCredits()
    {
        if (creditsTextRect == null)
        {
            Debug.LogError("CreditsManager_TimeBased: creditsTextRect não atribuído!");
            return;
        }

        RectTransform parentRect = creditsTextRect.parent as RectTransform;
        float screenHeight = parentRect.rect.height;
        float contentHeight = GetContentHeight();

        float startY = -screenHeight / 2f - contentHeight / 2f - 50f;
        creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, startY);

        float endY = screenHeight / 2f + contentHeight / 2f + extraEndPadding;
        scrollSpeed = (totalScrollDuration > 0f) ? (endY - startY) / totalScrollDuration : 0f;

        scrollTimer = 0f;
        isScrolling = true;
        centerSequenceStarted = false;
    }

    private float GetContentHeight()
    {
        var tmp = creditsTextRect.GetComponent<TMP_Text>();
        return tmp != null ? tmp.preferredHeight : creditsTextRect.rect.height;
    }

    private void Update()
    {
        if (isScrolling)
        {
            scrollTimer += Time.deltaTime;
            creditsTextRect.anchoredPosition += new Vector2(0f, scrollSpeed * Time.deltaTime);

            if (scrollTimer >= totalScrollDuration)
                EndScrollAndStartCenterMessages();
        }

        if (Input.GetKeyDown(skipKey))
        {
            if (isScrolling)
            {
                if (allowSkipToEnd)
                    EndScrollAndStartCenterMessages();
                else
                    SceneManager.LoadScene(mainMenuSceneName);
            }
            else if (centerSequenceStarted)
                SceneManager.LoadScene(mainMenuSceneName);
            else
                SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void EndScrollAndStartCenterMessages()
    {
        if (!isScrolling) return;
        isScrolling = false;

        RectTransform parentRect = creditsTextRect.parent as RectTransform;
        float screenHeight = parentRect.rect.height;
        float contentHeight = GetContentHeight();
        float finalY = screenHeight / 2f + contentHeight / 2f + extraEndPadding;
        creditsTextRect.anchoredPosition = new Vector2(creditsTextRect.anchoredPosition.x, finalY);

        StartCoroutine(PlayCenterMessages());
    }

    private IEnumerator PlayCenterMessages()
    {
        centerSequenceStarted = true;
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < centerMessages.Length; i++)
        {
            yield return StartCoroutine(SpawnAndAnimateCenterText(centerMessages[i]));
            yield return new WaitForSeconds(timeBetweenCenterMessages);
        }
    }

    private IEnumerator SpawnAndAnimateCenterText(string message)
    {
        GameObject go;

        if (centerTextPrefab == null)
        {
            go = new GameObject("CenterMessageTemp");
            go.transform.SetParent(creditsTextRect.parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 200);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 40;
            if (centerFont != null) tmp.font = centerFont;
        }
        else
        {
            // instanciando prefab sem sobrescrever posição
            go = Instantiate(centerTextPrefab, creditsTextRect.parent, false);
            go.name = "CenterMessage_" + message.Substring(0, Mathf.Min(12, message.Length)).Replace(" ", "_");

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
        }

        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, centerFadeIn));
        yield return new WaitForSeconds(centerHold);
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, centerFadeOut));

        Destroy(go);
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
