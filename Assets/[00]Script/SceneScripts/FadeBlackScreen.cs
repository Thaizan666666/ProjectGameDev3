using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class FadeBlackScreen : MonoBehaviour
{
    public static FadeBlackScreen Instance { get; private set; }

    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        var blackImage = GetComponentInChildren<Image>();
        if (blackImage != null)
        {
            var color = blackImage.color;
            color.a = 1f;
            blackImage.color = color;
        }
    }

    public void FadeIn(System.Action onComplete = null)
    {
        StopAllCoroutines();
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeRoutine(canvasGroup.alpha, 1f, fadeInDuration, onComplete));
    }

    public void FadeInThenLoadScene(string sceneName)
    {
        FadeIn(() =>
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.completed += _ => FadeOut();
        });
    }

    public void FadeOut(System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, fadeOutDuration, () =>
        {
            canvasGroup.blocksRaycasts = false;
            onComplete?.Invoke();
        }));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, System.Action onComplete)
    {
        yield return Fade(from, to, duration);
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
