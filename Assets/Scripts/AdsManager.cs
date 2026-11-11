using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

// Butun o'yin uchun bitta (sahnalar orasida saqlanadigan) reklama menejeri — Unity LevelPlay.
// Avtomatik yaratiladi — sahnaga qo'yish shart emas.
// Banner + mukofotli video reklama (Rewarded). Xatolar ekranda ham ko'rsatiladi.
public class AdsManager : MonoBehaviour
{
    // ====== SOZLAMALAR (LevelPlay dashboard'dan olinadi) ======
    // SINOV REJIMI: true bo'lsa, o'yin LevelPlay'ning rasmiy demo ilovasi orqali SINOV reklamalarini ko'rsatadi.
    // Ular har doim keladi, lekin pul bermaydi. Play Market'ga chiqarishdan oldin albatta false qiling!
    public const bool UseTestAds = true;

    // O'zingizning ilovangiz (LevelPlay dashboard'dan olinadi)
    const string RealAppKey = "29464f8c5";                    // LevelPlay → Apps → App Key
    const string RealBannerAdUnitId = "j6tnza2bb08mjj8h";     // LevelPlay → Ad units → "Rewarded" nomli (lekin formati Banner) unit
    const string RealRewardedAdUnitId = "slxi72eqd7uz6aev";   // LevelPlay → Ad units → "RewardedVideo"

    // LevelPlay hujjatlaridagi demo ilova (Android) — faqat sinov uchun
    const string DemoAppKey = "85460dcd";
    const string DemoBannerAdUnitId = "thnfvcsog13bhn08";
    const string DemoRewardedAdUnitId = "76yy3nay3ceui2a3";

    public static string AppKey => UseTestAds ? DemoAppKey : RealAppKey;
    public static string BannerAdUnitId => UseTestAds ? DemoBannerAdUnitId : RealBannerAdUnitId;
    public static string RewardedAdUnitId => UseTestAds ? DemoRewardedAdUnitId : RealRewardedAdUnitId;
    // Reklama yuklanmasa o'yinchi qotib qolmasligi uchun: true bo'lsa reklamasiz ham davom etadi
    public const bool ContinueWhenAdFails = true;
    // Xatolarni ekranda qizil panelda ko'rsatish (chiqarishda false qiling)
    public const bool ShowErrorsOnScreen = true;
    // Video tayyor bo'lmasa, o'yinchi tugmani bosganda shuncha soniya kutamiz
    const float WaitForAdSeconds = 10f;
    // Tekshirish uchun: true qilsangiz, o'yin ochilganda LevelPlay "Test Suite" oynasi chiqadi.
    // U yerda har bir reklama tarmog'i ulanganmi-yo'qmi ko'rinadi va sinov reklamasini yuklab ko'rish mumkin.
    // Chiqarishdan (release) oldin albatta false qiling!
    public const bool OpenTestSuite = false;
    // ==========================================================

    public static AdsManager Instance { get; private set; }

    bool sdkReady;
    string initStatus = "Reklama ishga tushirilmoqda (javob hali kelmadi)";
    LevelPlayRewardedAd rewardedAd;
    LevelPlayBannerAd bannerAd;
    Action pendingReward;
    bool rewardEarned;
    int loadRetry;
    int bannerRetry;
    bool rewardedLoading;
    float loadStartedAt;
    bool waitingForAd;

    Text errorText;
    GameObject errorPanel;
    float hideAt;
    readonly List<string> recent = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("AdsManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AdsManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildErrorUI();
    }

    void Start()
    {
        if (AppKey.StartsWith("YOUR_"))
        {
            initStatus = "LevelPlay App Key kiritilmagan (AdsManager.cs)";
            ShowError(initStatus);
            return;
        }
        // Avval eventlarga ulanamiz, keyin Init
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        Debug.Log($"[Ads] LevelPlay.Init, internet={Application.internetReachability}, sinov rejimi={UseTestAds}");
        if (OpenTestSuite) LevelPlay.SetMetaData("is_test_suite", "enable");
        if (Debug.isDebugBuild) LevelPlay.SetAdaptersDebug(true); // Development Build'da batafsil log
        LevelPlay.Init(AppKey);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        bannerAd?.DestroyAd();
    }

    void Update()
    {
        if (errorPanel.activeSelf && Time.unscaledTime > hideAt) { errorPanel.SetActive(false); recent.Clear(); }
    }

    // ---------------- Xatoni ekranda ko'rsatish ----------------
    public void ShowError(string message)
    {
        Debug.LogWarning("[Ads] " + message);
        if (!ShowErrorsOnScreen) return;
        Display(message);
    }

    // Oddiy xabar (masalan "Reklama yuklanmoqda"), har doim ko'rsatiladi
    void ShowInfo(string message)
    {
        Debug.Log("[Ads] " + message);
        Display(message);
    }

    void Display(string message)
    {
        // Oxirgi 3 ta xabarni ko'rsatamiz, birinchi (asl) xato yo'qolib ketmasligi uchun
        recent.Add(message);
        if (recent.Count > 3) recent.RemoveAt(0);
        errorText.text = string.Join("\n", recent);
        ((RectTransform)errorPanel.transform).sizeDelta = new Vector2(1000, 50 + 40 * recent.Count);
        errorPanel.SetActive(true);
        hideAt = Time.unscaledTime + 8f;
    }

    void BuildErrorUI()
    {
        var canvasGO = new GameObject("AdsErrorCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        errorPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        errorPanel.transform.SetParent(canvasGO.transform, false);
        var rt = errorPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -20);
        rt.sizeDelta = new Vector2(1000, 90);
        var img = errorPanel.GetComponent<Image>();
        img.color = new Color(0.6f, 0.05f, 0.05f, 0.9f);
        img.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(errorPanel.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(15, 5);
        trt.offsetMax = new Vector2(-15, -5);
        errorText = textGO.GetComponent<Text>();
        errorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        errorText.fontSize = 24;
        errorText.alignment = TextAnchor.MiddleCenter;
        errorText.color = Color.white;
        errorText.raycastTarget = false;
        errorPanel.SetActive(false);
    }

    // ---------------- Initialization ----------------
    void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("[Ads] LevelPlay ishga tushdi");
        sdkReady = true;
        initStatus = "";
        if (Debug.isDebugBuild) LevelPlay.ValidateIntegration(); // logcat'ga ulanish holatini yozadi
        if (OpenTestSuite) LevelPlay.LaunchTestSuite();
        CreateBanner();
        CreateRewarded();
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        initStatus = $"Reklama ishga tushmadi: {error.ErrorCode} — {error.ErrorMessage}";
        ShowError(initStatus);
    }

    // ---------------- Banner ----------------
    void CreateBanner()
    {
        if (BannerAdUnitId.StartsWith("YOUR_")) { ShowError("Banner Ad Unit ID kiritilmagan"); return; }
        var config = new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(true)
            .SetRespectSafeArea(true)
            .Build();
        bannerAd = new LevelPlayBannerAd(BannerAdUnitId, config);
        bannerAd.OnAdLoadFailed += error =>
        {
            ShowError($"Banner yuklanmadi: {error.ErrorCode} — {error.ErrorMessage}");
            // Qayta urinish: 15, 30, 60, 120 soniya
            float delay = Mathf.Min(15f * (1 << Mathf.Min(bannerRetry, 3)), 120f);
            bannerRetry++;
            CancelInvoke(nameof(LoadBanner));
            Invoke(nameof(LoadBanner), delay);
        };
        bannerAd.OnAdLoaded += info => bannerRetry = 0;
        bannerAd.OnAdDisplayFailed += (info, error) => ShowError($"Banner ko'rsatilmadi: {error.ErrorCode} — {error.ErrorMessage}");
        bannerAd.LoadAd();
    }

    void LoadBanner()
    {
        bannerAd?.LoadAd();
    }

    // ---------------- Mukofotli video ----------------
    void CreateRewarded()
    {
        if (RewardedAdUnitId.StartsWith("YOUR_")) { ShowError("Rewarded Ad Unit ID kiritilmagan"); return; }
        rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
        rewardedAd.OnAdLoadFailed += error =>
        {
            rewardedLoading = false;
            ShowError($"Video yuklanmadi: {error.ErrorCode} — {error.ErrorMessage}");
            // Qayta urinish: 5, 10, 20, 30 soniya (No fill vaqtincha bo'lishi mumkin)
            float delay = Mathf.Min(5f * (1 << Mathf.Min(loadRetry, 3)), 30f);
            loadRetry++;
            CancelInvoke(nameof(LoadRewarded));
            Invoke(nameof(LoadRewarded), delay);
        };
        rewardedAd.OnAdLoaded += info => { rewardedLoading = false; loadRetry = 0; };
        rewardedAd.OnAdDisplayFailed += (info, error) =>
        {
            ShowError($"Video ko'rsatilmadi: {error.ErrorCode} — {error.ErrorMessage}");
            var reward = pendingReward;
            pendingReward = null;
            LoadRewarded();
            Fallback(reward);
        };
        rewardedAd.OnAdRewarded += (info, reward) => rewardEarned = true;
        rewardedAd.OnAdClosed += info => StartCoroutine(FinishRewarded());
        LoadRewarded();
    }

    void LoadRewarded()
    {
        // Tayyor yoki yuklanayotgan bo'lsa, qayta so'ramaymiz
        // (30 soniyadan beri javob kelmagan bo'lsa, qotib qolmaslik uchun baribir qayta so'raymiz)
        bool stillLoading = rewardedLoading && Time.unscaledTime - loadStartedAt < 30f;
        if (rewardedAd == null || stillLoading || rewardedAd.IsAdReady()) return;
        rewardedLoading = true;
        loadStartedAt = Time.unscaledTime;
        rewardedAd.LoadAd();
    }

    // Game Over oynasi chiqqanda chaqiriladi: o'yinchi tugmani bosguncha video tayyor bo'lib tursin
    public void PrepareRewarded()
    {
        if (!sdkReady) return;
        CancelInvoke(nameof(LoadRewarded));
        LoadRewarded();
    }

    // Video reklamani ko'rsatadi. To'liq ko'rilsa onReward chaqiriladi.
    public void ShowRewarded(Action onReward)
    {
        if (!sdkReady || rewardedAd == null)
        {
            ShowError(string.IsNullOrEmpty(initStatus) ? "Reklama tizimi hali ishga tushmagan" : initStatus);
            Fallback(onReward);
            return;
        }
        if (!rewardedAd.IsAdReady())
        {
            // Darhol tashlab yubormaymiz: qisqa vaqt yuklanishini kutamiz
            if (!waitingForAd) StartCoroutine(WaitAndShow(onReward));
            return;
        }

        pendingReward = onReward;
        rewardEarned = false;
        rewardedAd.ShowAd();
    }

    IEnumerator WaitAndShow(Action onReward)
    {
        waitingForAd = true;
        ShowInfo("Reklama yuklanmoqda, kuting...");
        CancelInvoke(nameof(LoadRewarded));
        LoadRewarded();

        float t = 0f;
        while (t < WaitForAdSeconds && !rewardedAd.IsAdReady())
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        waitingForAd = false;

        if (rewardedAd.IsAdReady())
        {
            pendingReward = onReward;
            rewardEarned = false;
            rewardedAd.ShowAd();
        }
        else
        {
            ShowError("Video reklama topilmadi (No fill)");
            Fallback(onReward);
        }
    }

    // Mukofot eventi yopilish eventidan keyin ham kelishi mumkin, shuning uchun biroz kutamiz
    IEnumerator FinishRewarded()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        var reward = pendingReward;
        pendingReward = null;
        LoadRewarded();
        if (rewardEarned) reward?.Invoke();
        else if (reward != null) ShowError("Reklama oxirigacha ko'rilmadi");
    }

    void Fallback(Action onReward)
    {
        if (ContinueWhenAdFails) onReward?.Invoke();
    }
}
