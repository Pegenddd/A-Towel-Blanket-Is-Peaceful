using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HeartbeatManager : MonoBehaviour
{
    public enum GameState { Title, Playing, GameOver }

    public GameState currentState = GameState.Playing;

    public Heartbeat visualController;
    public Image darkOverlay;
    public float overlayLerpSpeed = 3.5f;

    [Range(0, 100)] public float currentWillpower = 85f;
    public float decayRate = 6.0f;
    public float willpowerGainPerfect = 14f;
    public float willpowerGainGood = 8f;
    public float willpowerPenaltyMiss = 10f;

    public float baseBeatInterval = 1.2f;
    public float fastBeatInterval = 0.8f;
    public float ringExpandSpeed = 1.6f;
    public float hitTolerance = 0.32f;
    private float beatTimer = 0f;
    private float currentBeatInterval = 1.2f;

    public int score = 0;
    public int scoreGainPerfect = 100;
    public int scoreGainGood = 50;

    public AudioSource heartbeatSource;
    public AudioClip heartBeatClip;

    private List<PulseRing> activeRings = new List<PulseRing>();
    private float targetOverlayAlpha = 0f;

    void Awake()
    {
        EnsureAudioComponents();
        EnsureVisualComponents();
    }

    void Start()
    {
        InitAudioClip();
        StartGame();
    }

    void EnsureAudioComponents()
    {
        if (heartbeatSource == null)
        {
            heartbeatSource = GetComponent<AudioSource>();
            if (heartbeatSource == null)
            {
                heartbeatSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void EnsureVisualComponents()
    {
        if (FindFirstObjectByType<EKGMonitor>() == null)
        {
            GameObject ekgObj = new GameObject("EKG_Monitor");
            ekgObj.AddComponent<EKGMonitor>();
        }

        if (FindFirstObjectByType<VisualJuice>() == null)
        {
            GameObject juiceObj = new GameObject("VisualJuice");
            juiceObj.AddComponent<VisualJuice>();
        }

        if (FindFirstObjectByType<CharacterSilhouette>() == null)
        {
            GameObject charObj = new GameObject("CharacterSilhouette");
            charObj.AddComponent<CharacterSilhouette>();
        }

        if (darkOverlay != null && darkOverlay.sprite == null)
        {
            darkOverlay.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    void InitAudioClip()
    {
        if (heartBeatClip == null)
        {
            heartBeatClip = SoundSynthesizer.CreateHeartbeatClip();
        }
    }

    public void StartGame()
    {
        currentWillpower = 85f;
        score = 0;
        beatTimer = 0.4f;
        currentState = GameState.Playing;

        foreach (var r in activeRings)
        {
            if (r != null) Destroy(r.gameObject);
        }
        activeRings.Clear();
    }

    void Update()
    {
        if (currentState == GameState.Title || currentState == GameState.GameOver)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartGame();
            }
            UpdateVisuals();
            return;
        }

        HandleBeatTiming();
        HandleWillpowerDecay();
        HandlePlayerInput();
        CleanUpRings();
        UpdateVisuals();
    }

    void HandleBeatTiming()
    {
        float normalizedHealth = currentWillpower / 100f;
        currentBeatInterval = Mathf.Lerp(fastBeatInterval, baseBeatInterval, normalizedHealth);

        beatTimer += Time.deltaTime;
        if (beatTimer >= currentBeatInterval)
        {
            beatTimer = 0f;
            TriggerPulseWave();
        }
    }

    void TriggerPulseWave()
    {
        if (visualController != null)
        {
            visualController.TriggerPulse();
            PulseRing newRing = visualController.SpawnPulseRing(ringExpandSpeed);
            if (newRing != null)
            {
                activeRings.Add(newRing);
            }
        }

        if (heartbeatSource != null && heartBeatClip != null)
        {
            heartbeatSource.PlayOneShot(heartBeatClip, 1.0f);
        }
    }

    void HandleWillpowerDecay()
    {
        currentWillpower -= decayRate * Time.deltaTime;
        currentWillpower = Mathf.Clamp(currentWillpower, 0f, 100f);

        if (currentWillpower <= 0f)
        {
            TriggerGameOver();
        }
    }

    void HandlePlayerInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        PulseRing closestRing = null;
        float minDistance = float.MaxValue;
        float targetScale = (visualController != null) ? visualController.targetRingScale : 1.65f;

        foreach (var ring in activeRings)
        {
            if (ring == null || ring.state != PulseRing.RingState.Active) continue;
            float dist = Mathf.Abs(ring.currentScale - targetScale);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestRing = ring;
            }
        }

        if (closestRing != null && minDistance <= hitTolerance)
        {
            bool isPerfect = minDistance <= (hitTolerance * 0.45f);

            if (isPerfect)
            {
                currentWillpower += willpowerGainPerfect;
                score += scoreGainPerfect;

                closestRing.OnHit(true);
                if (visualController != null) visualController.OnHitPerfect();
                if (EKGMonitor.Instance != null) EKGMonitor.Instance.OnPlayerHit(true);
            }
            else
            {
                currentWillpower += willpowerGainGood;
                score += scoreGainGood;

                closestRing.OnHit(false);
                if (visualController != null) visualController.OnHitGood();
                if (EKGMonitor.Instance != null) EKGMonitor.Instance.OnPlayerHit(false);
            }
        }
        else
        {
            currentWillpower -= willpowerPenaltyMiss;

            if (visualController != null) visualController.OnHitMiss();
            if (EKGMonitor.Instance != null) EKGMonitor.Instance.OnPlayerMiss();
        }

        currentWillpower = Mathf.Clamp(currentWillpower, 0f, 100f);
        if (currentWillpower <= 0f)
        {
            TriggerGameOver();
        }
    }

    public void OnRingPassedMiss()
    {
        if (currentState != GameState.Playing) return;

        currentWillpower -= (willpowerPenaltyMiss * 0.8f);

        if (visualController != null) visualController.OnHitMiss();
        if (EKGMonitor.Instance != null) EKGMonitor.Instance.OnPlayerMiss();

        currentWillpower = Mathf.Clamp(currentWillpower, 0f, 100f);
        if (currentWillpower <= 0f)
        {
            TriggerGameOver();
        }
    }

    void CleanUpRings()
    {
        activeRings.RemoveAll(r => r == null);
    }

    void UpdateVisuals()
    {
        float normalizedHealth = currentWillpower / 100f;

        if (darkOverlay != null)
        {
            targetOverlayAlpha = (currentState == GameState.GameOver) ? 0.98f : (1f - normalizedHealth * 0.92f);
            Color c = darkOverlay.color;
            c.a = Mathf.Lerp(c.a, targetOverlayAlpha, Time.deltaTime * overlayLerpSpeed);
            darkOverlay.color = c;
        }
    }

    void TriggerGameOver()
    {
        currentState = GameState.GameOver;
        currentWillpower = 0f;

        Debug.Log($"[Heartbeat Game] Game Over! Final Score: {score}");
    }

    void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;

        GUIStyle styleScore = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(16, (int)(sh * 0.035f)),
            alignment = TextAnchor.UpperLeft,
            fontStyle = FontStyle.Bold
        };
        styleScore.normal.textColor = new Color(1f, 0.75f, 0.88f, 0.9f);

        if (currentState == GameState.Playing)
        {
            GUI.Label(new Rect(30, 20, 300, 40), $"SCORE: {score}", styleScore);

            float barWidth = Mathf.Min(380, sw * 0.52f);
            float barHeight = 8f;
            float barX = (sw - barWidth) / 2f;
            float barY = sh - 42f;

            GUI.color = new Color(0.18f, 0.08f, 0.14f, 0.6f);
            GUI.DrawTexture(new Rect(barX - 2, barY - 2, barWidth + 4, barHeight + 4), Texture2D.whiteTexture);

            float fillRatio = currentWillpower / 100f;
            Color barColor = Color.Lerp(new Color(0.9f, 0.15f, 0.3f), new Color(1f, 0.58f, 0.78f), fillRatio);
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barX, barY, barWidth * fillRatio, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else if (currentState == GameState.GameOver)
        {
            GUIStyle styleRestart = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(16, (int)(sh * 0.035f)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            styleRestart.normal.textColor = new Color(1f, 0.82f, 0.92f, 0.85f);

            float cx = sw / 2f;
            float cy = sh / 2f;

            GUI.Label(new Rect(cx - 220, cy - 20, 440, 40), "[ CLICK TO RESTART ]", styleRestart);
        }
    }
}

public class EKGMonitor : MonoBehaviour
{
    public static EKGMonitor Instance { get; private set; }

    public int resolution = 140;
    public float monitorWidth = 8.8f;
    public float monitorHeight = 0.95f;
    public Vector3 offset = new Vector3(0, 3.6f, 0);

    private LineRenderer coreLine;
    private float[] ekgBuffer;

    private Queue<float> sampleQueue = new Queue<float>();
    private float feedTimer = 0f;
    private float samplesPerSecond = 55f;

    private Color currentLineColor = new Color(1f, 0.45f, 0.65f, 1f);
    private Color targetLineColor;

    void Awake()
    {
        if (Instance == null) Instance = this;

        transform.position = offset;
        ekgBuffer = new float[resolution];

        GameObject coreObj = new GameObject("EKG_Core");
        coreObj.transform.SetParent(transform);
        coreObj.transform.localPosition = Vector3.zero;
        coreLine = coreObj.AddComponent<LineRenderer>();
        SetupLineRenderer(coreLine, 0.055f, 14);

        targetLineColor = currentLineColor;
    }

    void SetupLineRenderer(LineRenderer lr, float width, int sortOrder)
    {
        lr.positionCount = resolution;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = false;
        lr.sortingOrder = sortOrder;
        lr.material = new Material(Shader.Find("Sprites/Default"));
    }

    void Update()
    {
        HeartbeatManager manager = FindFirstObjectByType<HeartbeatManager>();
        float vitality01 = (manager != null) ? manager.currentWillpower / 100f : 1f;

        feedTimer += Time.deltaTime;
        float stepTime = 1f / samplesPerSecond;

        while (feedTimer >= stepTime)
        {
            feedTimer -= stepTime;
            FeedNextSample(vitality01);
        }

        float halfWidth = monitorWidth / 2f;
        for (int i = 0; i < resolution; i++)
        {
            float x = Mathf.Lerp(-halfWidth, halfWidth, (float)i / (resolution - 1));
            float y = ekgBuffer[i] * monitorHeight;
            Vector3 pos = new Vector3(x, y, 0);

            coreLine.SetPosition(i, pos);
        }

        currentLineColor = Color.Lerp(currentLineColor, targetLineColor, Time.deltaTime * 6f);
        coreLine.startColor = currentLineColor;
        coreLine.endColor = currentLineColor;
    }

    void FeedNextSample(float vitality01)
    {
        for (int i = 0; i < resolution - 1; i++)
        {
            ekgBuffer[i] = ekgBuffer[i + 1];
        }

        float newSample = 0f;
        if (sampleQueue.Count > 0)
        {
            newSample = sampleQueue.Dequeue();
        }
        else
        {
            newSample = (Mathf.PerlinNoise(Time.time * 6f, 0f) - 0.5f) * 0.04f * vitality01;
        }

        ekgBuffer[resolution - 1] = newSample;
    }

    public void OnPlayerHit(bool isPerfect)
    {
        float amplitude = isPerfect ? 1.0f : 0.75f;

        float[] spikeSamples = new float[]
        {
            0.05f * amplitude,
            0.12f * amplitude,
            0.18f * amplitude,
            0.08f * amplitude,
            -0.02f,
            -0.35f * amplitude,
            0.45f * amplitude,
            1.0f * amplitude,
            0.6f * amplitude,
            -0.45f * amplitude,
            -0.1f * amplitude,
            0.05f * amplitude,
            0.18f * amplitude,
            0.32f * amplitude,
            0.20f * amplitude,
            0.06f * amplitude,
            0.0f
        };

        foreach (float s in spikeSamples)
        {
            sampleQueue.Enqueue(s);
        }

        if (isPerfect)
        {
            currentLineColor = new Color(1f, 0.9f, 0.45f, 1f);
            targetLineColor = new Color(1f, 0.5f, 0.7f, 1f);
        }
        else
        {
            currentLineColor = new Color(1f, 0.6f, 0.8f, 1f);
            targetLineColor = new Color(1f, 0.35f, 0.55f, 1f);
        }
    }

    public void OnPlayerMiss()
    {
        float[] missSamples = new float[]
        {
            -0.15f, -0.3f, -0.2f, -0.05f, 0.05f, -0.1f, 0f
        };

        foreach (float s in missSamples)
        {
            sampleQueue.Enqueue(s);
        }

        currentLineColor = new Color(0.4f, 0.35f, 0.75f, 0.8f);
        targetLineColor = new Color(0.6f, 0.25f, 0.35f, 0.7f);
    }
}

public class CharacterSilhouette : MonoBehaviour
{
    private Vector3 initialPos;

    void Awake()
    {
        transform.position = new Vector3(0f, -2.6f, 0f);
        initialPos = transform.position;
    }

    void Update()
    {
        HeartbeatManager manager = FindFirstObjectByType<HeartbeatManager>();
        float vitality01 = (manager != null) ? manager.currentWillpower / 100f : 1f;

        float breatheSpeed = Mathf.Lerp(1.5f, 2.5f, 1f - vitality01);
        float breatheOffset = Mathf.Sin(Time.time * breatheSpeed) * 0.05f * vitality01;
        transform.position = initialPos + new Vector3(0f, breatheOffset, 0f);
    }
}

public class VisualJuice : MonoBehaviour
{
    public static VisualJuice Instance { get; private set; }

    private Camera targetCamera;
    private float baseOrthoSize = 5f;
    private Vector3 cameraInitialPos;
    private float shakeIntensity = 0f;
    private float shakeDecay = 6f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        targetCamera = Camera.main;
        if (targetCamera != null)
        {
            baseOrthoSize = targetCamera.orthographicSize;
            cameraInitialPos = targetCamera.transform.position;
        }
    }

    void Update()
    {
        HeartbeatManager manager = FindFirstObjectByType<HeartbeatManager>();
        float vitality01 = (manager != null) ? manager.currentWillpower / 100f : 1f;

        UpdateCamera(vitality01);
    }

    void UpdateCamera(float vitality01)
    {
        if (targetCamera == null) return;

        if (shakeIntensity > 0f)
        {
            Vector3 shakeOffset = (Vector3)Random.insideUnitCircle * shakeIntensity;
            targetCamera.transform.position = cameraInitialPos + shakeOffset;
            shakeIntensity = Mathf.Max(0f, shakeIntensity - Time.deltaTime * shakeDecay);
        }
        else
        {
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, cameraInitialPos, Time.deltaTime * 8f);
        }

        float targetZoom = baseOrthoSize - Mathf.Sin(Time.time * 2.5f) * (0.04f + (1f - vitality01) * 0.08f);
        targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, targetZoom, Time.deltaTime * 4f);
    }

    public void AddShake(float amount)
    {
        shakeIntensity = Mathf.Min(shakeIntensity + amount, 0.3f);
    }
}

public static class ProceduralVisuals
{
    private static Sprite cachedHeartSprite;
    private static Sprite cachedTargetRingSprite;
    private static Sprite cachedPulseRingSprite;
    private static Sprite cachedSoftGlowSprite;

    private static readonly Color C_BLANK = Color.clear;
    private static readonly Color C_BLACK = new Color(0.04f, 0.04f, 0.04f, 1f);
    private static readonly Color C_RED   = new Color(1.0f, 0.0f, 0.0f, 1f);
    private static readonly Color C_WHITE = new Color(1.0f, 1.0f, 1.0f, 1f);
    private static readonly Color C_DARK  = new Color(0.55f, 0.04f, 0.04f, 1f);

    private static readonly string[] PIXEL_HEART_MAP = new string[]
    {
        ". . B B B . . . . B B B . . . .",
        ". B R R R B . . B R R R D B . .",
        "B R W W R R B B R R R R D D B .",
        "B R W W R R R R R R R R R D B .",
        "B R R R R R R R R R R R R D B .",
        "B R W W R R R R R R R R R D B .",
        "B R W W R R R R R R R R D D B .",
        ". B R R R R R R R R R R D B . .",
        ". B R R R R R R R R R D D B . .",
        ". . B R R R R R R R R D B . . .",
        ". . B R R R R R R R D D B . . .",
        ". . . B R R R R R R D B . . . .",
        ". . . . B R R R R D B . . . . .",
        ". . . . . B R R D B . . . . . .",
        ". . . . . . B D B . . . . . . .",
        ". . . . . . . B . . . . . . . ."
    };

    public static Sprite GetHeartSprite(int scaleMultiplier = 16)
    {
        if (cachedHeartSprite != null) return cachedHeartSprite;

        int rawRows = PIXEL_HEART_MAP.Length;
        int rawCols = 16;
        int width = rawCols * scaleMultiplier;
        int height = rawRows * scaleMultiplier;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];

        for (int r = 0; r < rawRows; r++)
        {
            int texY_base = (rawRows - 1 - r) * scaleMultiplier;
            string[] tokens = PIXEL_HEART_MAP[r].Split(' ');

            for (int c = 0; c < rawCols && c < tokens.Length; c++)
            {
                Color pixelCol = ParseColorCode(tokens[c]);
                int texX_base = c * scaleMultiplier;

                for (int dy = 0; dy < scaleMultiplier; dy++)
                {
                    for (int dx = 0; dx < scaleMultiplier; dx++)
                    {
                        int px = texX_base + dx;
                        int py = texY_base + dy;
                        pixels[py * width + px] = pixelCol;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedHeartSprite = Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.45f),
            100f
        );

        return cachedHeartSprite;
    }

    private static Color ParseColorCode(string code)
    {
        switch (code.Trim().ToUpper())
        {
            case "B": return C_BLACK;
            case "R": return C_RED;
            case "W": return C_WHITE;
            case "D": return C_DARK;
            default:  return C_BLANK;
        }
    }

    public static Sprite GetTargetRingSprite(int resolution = 512)
    {
        if (cachedTargetRingSprite != null) return cachedTargetRingSprite;

        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = resolution / 2f;
        float targetR = center * 0.78f;
        float perfectTolerance = center * 0.05f;
        float goodTolerance = center * 0.16f;

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float delta = Mathf.Abs(dist - targetR);

                Color pixelColor = Color.clear;

                if (delta <= goodTolerance)
                {
                    float perfectSharpness = Mathf.Exp(-delta * delta / (perfectTolerance * perfectTolerance * 1.8f));
                    float goodNorm = delta / goodTolerance;
                    float goodGlow = Mathf.Pow(1f - goodNorm, 1.6f) * 0.45f;

                    float innerEdge = Mathf.Exp(-Mathf.Pow(dist - (targetR - goodTolerance * 0.85f), 2f) * 0.15f) * 0.5f;
                    float outerEdge = Mathf.Exp(-Mathf.Pow(dist - (targetR + goodTolerance * 0.85f), 2f) * 0.15f) * 0.5f;

                    float totalAlpha = Mathf.Clamp01(perfectSharpness * 0.95f + goodGlow + innerEdge + outerEdge);

                    Color baseRingColor = Color.Lerp(
                        new Color(1f, 0.95f, 0.7f),
                        new Color(1f, 0.4f, 0.65f),
                        goodNorm
                    );

                    float angle = Mathf.Atan2(dy, dx);
                    float crossAlignment = Mathf.Abs(Mathf.Sin(angle * 2f));
                    if (crossAlignment < 0.08f)
                    {
                        totalAlpha = Mathf.Min(1f, totalAlpha + 0.35f);
                        baseRingColor = Color.Lerp(baseRingColor, Color.white, 0.6f);
                    }

                    baseRingColor.a = totalAlpha;
                    pixelColor = baseRingColor;
                }

                pixels[y * resolution + x] = pixelColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedTargetRingSprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        return cachedTargetRingSprite;
    }

    public static Sprite GetGlowRingSprite(int resolution = 256)
    {
        if (cachedPulseRingSprite != null) return cachedPulseRingSprite;

        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = resolution / 2f;
        float targetR = center * 0.82f;
        float ringThickness = center * 0.16f;

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float delta = Mathf.Abs(dist - targetR);

                if (delta <= ringThickness)
                {
                    float alpha = Mathf.Exp(-delta * delta * 0.04f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedPulseRingSprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        return cachedPulseRingSprite;
    }

    public static Sprite GetSoftGlowSprite(int resolution = 256)
    {
        if (cachedSoftGlowSprite != null) return cachedSoftGlowSprite;

        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = resolution / 2f;
        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 2.2f);
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedSoftGlowSprite = Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), 100f);
        return cachedSoftGlowSprite;
    }
}

public static class SoundSynthesizer
{
    private static int sampleRate = 44100;

    public static AudioClip CreateHeartbeatClip()
    {
        float duration = 0.45f;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        float[] thumpTimes = { 0.0f, 0.16f };
        float thumpDuration = 0.14f;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;

            foreach (float startTime in thumpTimes)
            {
                if (t >= startTime && t < startTime + thumpDuration)
                {
                    float localT = (t - startTime) / thumpDuration;
                    float freq = Mathf.Lerp(65f, 28f, localT);
                    float env = Mathf.Sin(localT * Mathf.PI);
                    sample += Mathf.Sin(2f * Mathf.PI * freq * (t - startTime)) * env * 0.85f;
                }
            }

            samples[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Procedural_Heartbeat", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}