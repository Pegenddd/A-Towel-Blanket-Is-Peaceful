using UnityEngine;

public class Heartbeat : MonoBehaviour
{
    public Transform heartSprite;
    public Transform ringSprite;

    public float baseScale = 1.0f;
    public float pulseMaxScale = 1.2f;
    public float targetRingScale = 0.95f;

    public Color normalHeartColor = Color.white;
    public Color hitPerfectColor = new Color(1f, 0.9f, 0.45f, 1f);
    public Color hitGoodColor = new Color(1f, 0.52f, 0.78f, 1f);
    public Color missColor = new Color(0.28f, 0.28f, 0.72f, 1f);
    public float flashDuration = 0.16f;

    private SpriteRenderer heartRenderer;
    private SpriteRenderer ringRenderer;

    private float currentHeartScale = 1.0f;
    private float currentRingScale = 1.65f;
    private float flashTimer = 0f;
    private Color currentFlashColor;
    private Color currentRingColor = Color.white;

    void Awake()
    {
        if (heartSprite != null)
        {
            heartSprite.gameObject.SetActive(false);
        }
        Transform childHeart = transform.Find("HeartVisual");
        if (childHeart != null)
        {
            childHeart.gameObject.SetActive(false);
        }

        SetupTargetJudgmentRing();

        currentHeartScale = baseScale;
        currentRingScale = targetRingScale;
    }

    void SetupTargetJudgmentRing()
    {
        if (ringSprite == null)
        {
            GameObject rObj = new GameObject("TargetJudgmentRing");
            rObj.transform.SetParent(transform);
            rObj.transform.localPosition = Vector3.zero;
            ringSprite = rObj.transform;
        }
        else
        {
            ringSprite.localPosition = Vector3.zero;
            ringSprite.gameObject.SetActive(true);
        }

        ringRenderer = ringSprite.GetComponent<SpriteRenderer>();
        if (ringRenderer == null)
        {
            ringRenderer = ringSprite.gameObject.AddComponent<SpriteRenderer>();
        }

        ringRenderer.sprite = ProceduralVisuals.GetTargetRingSprite();
        ringRenderer.sortingOrder = 5;
        ringRenderer.color = new Color(1f, 0.95f, 0.8f, 0.75f);
        ringSprite.localScale = Vector3.one * targetRingScale;
    }

    void Update()
    {
        HeartbeatManager manager = FindFirstObjectByType<HeartbeatManager>();
        float vitality01 = (manager != null) ? manager.currentWillpower / 100f : 1f;

        currentHeartScale = Mathf.Lerp(currentHeartScale, baseScale, Time.deltaTime * 6.0f);
        if (heartSprite != null)
        {
            heartSprite.localScale = Vector3.one * currentHeartScale;
        }

        currentRingScale = targetRingScale;
        if (ringSprite != null)
        {
            ringSprite.localScale = Vector3.one * currentRingScale;
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(flashTimer / flashDuration);

            if (heartRenderer != null)
                heartRenderer.color = Color.Lerp(currentFlashColor, normalHeartColor, t);

            if (ringRenderer != null)
                ringRenderer.color = Color.Lerp(currentRingColor, new Color(1f, 0.95f, 0.85f, 0.85f), t);
        }
        else
        {
            if (ringRenderer != null)
            {
                Color normalRingCol = Color.Lerp(
                    new Color(0.6f, 0.25f, 0.35f, 0.35f),
                    new Color(1f, 0.95f, 0.85f, 0.85f),
                    vitality01
                );
                ringRenderer.color = normalRingCol;
            }
        }
    }

    public void TriggerPulse()
    {
        currentHeartScale = pulseMaxScale;
        if (VisualJuice.Instance != null)
        {
            VisualJuice.Instance.AddShake(0.04f);
        }
    }

    public Color normalRingColor = new Color(1f, 0.95f, 0.8f, 0.75f);

    public PulseRing SpawnPulseRing(float expandSpeed, float startScale = 0.15f, Color? color = null)
    {
        GameObject ringObj = new GameObject("PulseRing");
        ringObj.transform.position = transform.position;

        PulseRing pulse = ringObj.AddComponent<PulseRing>();
        Sprite spriteToUse = ProceduralVisuals.GetGlowRingSprite();

        Color col = color ?? new Color(1f, 0.45f, 0.65f, 0.8f);
        pulse.Initialize(spriteToUse, startScale, targetRingScale, expandSpeed, col);
        return pulse;
    }

    public void OnHitPerfect()
    {
        currentHeartScale = pulseMaxScale * 1.32f;
        currentRingScale = targetRingScale * 1.08f;
        currentFlashColor = hitPerfectColor;
        currentRingColor = new Color(1f, 0.92f, 0.45f, 1f);
        flashTimer = flashDuration * 1.35f;

        if (heartRenderer != null) heartRenderer.color = currentFlashColor;
        if (ringRenderer != null) ringRenderer.color = currentRingColor;

        if (VisualJuice.Instance != null)
        {
            VisualJuice.Instance.AddShake(0.10f);
        }
    }

    public void OnHitGood()
    {
        currentHeartScale = pulseMaxScale * 1.15f;
        currentRingScale = targetRingScale * 1.04f;
        currentFlashColor = hitGoodColor;
        currentRingColor = new Color(1f, 0.55f, 0.78f, 0.95f);
        flashTimer = flashDuration;

        if (heartRenderer != null) heartRenderer.color = currentFlashColor;
        if (ringRenderer != null) ringRenderer.color = currentRingColor;

        if (VisualJuice.Instance != null)
        {
            VisualJuice.Instance.AddShake(0.05f);
        }
    }

    public void OnHitMiss()
    {
        currentHeartScale = baseScale * 0.88f;
        currentRingScale = targetRingScale * 0.96f;
        currentFlashColor = missColor;
        currentRingColor = new Color(0.35f, 0.35f, 0.75f, 0.75f);
        flashTimer = flashDuration;

        if (heartRenderer != null) heartRenderer.color = currentFlashColor;
        if (ringRenderer != null) ringRenderer.color = currentRingColor;

        if (VisualJuice.Instance != null)
        {
            VisualJuice.Instance.AddShake(0.08f);
        }
    }
}

public class PulseRing : MonoBehaviour
{
    public enum RingState { Active, Hit, Missed }

    [HideInInspector] public RingState state = RingState.Active;
    [HideInInspector] public float currentScale = 0.15f;
    [HideInInspector] public float targetScale = 1.65f;
    [HideInInspector] public float expandSpeed = 1.6f;

    private SpriteRenderer spriteRenderer;
    private Color baseColor = new Color(1f, 0.45f, 0.65f, 0.75f);
    private float alpha = 0.75f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sortingOrder = 6;
    }

    public void Initialize(Sprite ringSprite, float startScale, float targetScaleVal, float speed, Color color)
    {
        if (ringSprite == null)
        {
            ringSprite = ProceduralVisuals.GetGlowRingSprite();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = ringSprite;
        }

        currentScale = startScale;
        targetScale = targetScaleVal;
        expandSpeed = speed;
        baseColor = color;
        alpha = color.a;
        transform.localScale = Vector3.one * currentScale;
        state = RingState.Active;
    }

    void Update()
    {
        if (state == RingState.Active)
        {
            currentScale += expandSpeed * Time.deltaTime;
            transform.localScale = Vector3.one * currentScale;

            float dist = Mathf.Abs(currentScale - targetScale);
            if (dist < 0.22f)
            {
                float t = 1f - (dist / 0.22f);
                Color activeHighlight = Color.Lerp(baseColor, new Color(1f, 0.85f, 0.95f, 0.95f), t);
                if (spriteRenderer != null) spriteRenderer.color = activeHighlight;
            }
            else
            {
                if (spriteRenderer != null) spriteRenderer.color = baseColor;
            }

            if (currentScale >= targetScale)
            {
                OnAutoMiss();
            }
        }
        else
        {
            alpha -= Time.deltaTime * 3.5f;
            currentScale += expandSpeed * 0.3f * Time.deltaTime;
            transform.localScale = Vector3.one * currentScale;

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Max(0f, alpha);
                spriteRenderer.color = c;
            }

            if (alpha <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public void OnHit(bool isPerfect)
    {
        state = RingState.Hit;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isPerfect 
                ? new Color(1f, 0.9f, 0.4f, 1f)
                : new Color(1f, 0.45f, 0.75f, 1f);
        }
    }

    public void OnAutoMiss()
    {
        if (state != RingState.Active) return;
        state = RingState.Missed;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.35f, 0.15f, 0.25f, 0.4f);
        }

        HeartbeatManager manager = FindFirstObjectByType<HeartbeatManager>();
        if (manager != null)
        {
            manager.OnRingPassedMiss();
        }
    }
}