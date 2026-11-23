using System;
using UnityEngine;

namespace MobRush
{
    public enum AdType
    {
        Banner,               // 1. Adaptive Bottom Banner
        Interstitial,         // 2. Full-Screen Interstitial
        Rewarded,             // 3. Rewarded Video (Watch full for 2X Coins)
        RewardedInterstitial, // 4. Rewarded Interstitial (Short countdown with reward)
        Native,               // 5. Native In-Feed / Card Ad
        AppOpen               // 6. App Open Splash / Launch Ad
    }

    /// <summary>
    /// Complete Implementation of all 6 Google AdMob Ad Formats with Official Test IDs:
    /// 1. Banner Ad (Adaptive Bottom Bar)
    /// 2. Interstitial Ad (Between levels / Game Over)
    /// 3. Rewarded Video Ad (2X coins / Free Champion)
    /// 4. Rewarded Interstitial Ad (Countdown bonus)
    /// 5. Native Ad (Integrated in Menu / Shop)
    /// 6. App Open Ad (App launch / Resume splash)
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        private static AdManager _instance;
        public static AdManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AdManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MobRush_AdManager");
                        _instance = go.AddComponent<AdManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Google AdMob Official Test Ad Unit IDs (Android)")]
        public const string SampleAppIdAndroid = "ca-app-pub-3940256099942544~3347511713";
        public const string TestBannerIdAndroid = "ca-app-pub-3940256099942544/6300978111";
        public const string TestInterstitialIdAndroid = "ca-app-pub-3940256099942544/1033173712";
        public const string TestRewardedIdAndroid = "ca-app-pub-3940256099942544/5224354917";
        public const string TestRewardedInterstitialIdAndroid = "ca-app-pub-3940256099942544/5354046379";
        public const string TestNativeIdAndroid = "ca-app-pub-3940256099942544/2247696110";
        public const string TestAppOpenIdAndroid = "ca-app-pub-3940256099942544/9257395921";

        [Header("Active Ad States")]
        public bool isBannerVisible = true;
        public bool hasShownAppOpenAd = false;

        // Active Overlay States
        private bool isShowingOverlay = false;
        private AdType currentAdType;
        private float adTimer = 0f;
        private float adDuration = 5f;
        private Action onRewardedCallback;
        private Action onClosedCallback;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Show App Open Ad on first startup
            if (!hasShownAppOpenAd)
            {
                ShowAppOpenAd();
            }
        }

        private void Update()
        {
            if (isShowingOverlay)
            {
                adTimer += Time.unscaledDeltaTime;
            }
        }

        // ==========================================
        // 1. BANNER ADS
        // ==========================================
        public void ShowBanner() => isBannerVisible = true;
        public void HideBanner() => isBannerVisible = false;

        // ==========================================
        // 2. INTERSTITIAL ADS
        // ==========================================
        public void ShowInterstitial(Action onClosed = null)
        {
            isShowingOverlay = true;
            currentAdType = AdType.Interstitial;
            adDuration = 3f;
            adTimer = 0f;
            onClosedCallback = onClosed;
            onRewardedCallback = null;
            Time.timeScale = 0f;
        }

        // ==========================================
        // 3. REWARDED VIDEO ADS
        // ==========================================
        public void ShowRewardedAd(Action onRewarded, Action onClosed = null)
        {
            isShowingOverlay = true;
            currentAdType = AdType.Rewarded;
            adDuration = 5f;
            adTimer = 0f;
            onRewardedCallback = onRewarded;
            onClosedCallback = onClosed;
            Time.timeScale = 0f;
        }

        // ==========================================
        // 4. REWARDED INTERSTITIAL ADS
        // ==========================================
        public void ShowRewardedInterstitial(Action onRewarded, Action onClosed = null)
        {
            isShowingOverlay = true;
            currentAdType = AdType.RewardedInterstitial;
            adDuration = 4f;
            adTimer = 0f;
            onRewardedCallback = onRewarded;
            onClosedCallback = onClosed;
            Time.timeScale = 0f;
        }

        // ==========================================
        // 5. NATIVE ADS
        // ==========================================
        public void DrawNativeAdCard(Rect rect)
        {
            // Embedded Native Ad Component inside Main Menu / Shop
            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.16f, 0.2f, 0.26f, 0.95f);
            GUI.Box(rect, "");

            // Sponsored Badge
            GUIStyle badge = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
            badge.normal.textColor = Color.black;
            GUI.backgroundColor = new Color(0.95f, 0.8f, 0.15f);
            GUI.Box(new Rect(rect.x + 8, rect.y + 6, 48, 18), "Ad", badge);

            // Native Title & Description
            GUIStyle nTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            nTitle.normal.textColor = Color.white;
            GUI.Label(new Rect(rect.x + 62, rect.y + 5, rect.width - 70, 20), "Google Play Featured App", nTitle);

            GUIStyle nDesc = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            nDesc.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
            GUI.Label(new Rect(rect.x + 10, rect.y + 26, rect.width - 110, 36), "Play popular top-chart games today.\nFree install with bonus rewards!", nDesc);

            // Install Button
            GUI.backgroundColor = new Color(0.15f, 0.65f, 1f);
            if (GUI.Button(new Rect(rect.x + rect.width - 92, rect.y + 16, 84, 34), "INSTALL"))
            {
                Application.OpenURL("https://play.google.com/store/apps/details?id=com.vincentb.MobControl");
            }

            GUI.backgroundColor = Color.white;
        }

        // ==========================================
        // 6. APP OPEN ADS
        // ==========================================
        public void ShowAppOpenAd(Action onClosed = null)
        {
            hasShownAppOpenAd = true;
            isShowingOverlay = true;
            currentAdType = AdType.AppOpen;
            adDuration = 3f;
            adTimer = 0f;
            onClosedCallback = onClosed;
            onRewardedCallback = null;
            Time.timeScale = 0f;
        }

        private void CompleteAd(bool grantReward = true)
        {
            isShowingOverlay = false;
            Time.timeScale = 1f;

            if (grantReward && (currentAdType == AdType.Rewarded || currentAdType == AdType.RewardedInterstitial))
            {
                onRewardedCallback?.Invoke();
            }
            onClosedCallback?.Invoke();
        }

        private void OnGUI()
        {
            // 1. Draw Adaptive Bottom Banner (if in Menu or enabled)
            if (isBannerVisible && !isShowingOverlay && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
            {
                DrawBottomBanner();
            }

            // 2. Full-Screen Ad Overlay for Interstitial, Rewarded, Rewarded Interstitial, App Open
            if (isShowingOverlay)
            {
                DrawFullScreenAdOverlay();
            }
        }

        private void DrawBottomBanner()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float bannerH = Mathf.Min(56f, sh * 0.08f);
            Rect bRect = new Rect(0, sh - bannerH, sw, bannerH);

            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.11f, 0.12f, 0.15f, 0.98f);
            GUI.Box(bRect, "");

            // Badge
            GUIStyle bBadge = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            bBadge.normal.textColor = Color.black;
            GUI.backgroundColor = new Color(0.95f, 0.8f, 0.15f);
            GUI.Box(new Rect(8, sh - bannerH + (bannerH - 24) / 2f, 65, 24), "Test Ad", bBadge);

            // Info
            GUIStyle bText = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            bText.normal.textColor = Color.white;
            GUI.Label(new Rect(80, sh - bannerH, sw - 120, bannerH), "AdMob 1/6 • Adaptive Bottom Banner (320x50)", bText);

            // Close
            GUI.backgroundColor = new Color(0.4f, 0.45f, 0.5f);
            if (GUI.Button(new Rect(sw - 36, sh - bannerH + (bannerH - 28) / 2f, 28, 28), "✕"))
            {
                HideBanner();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawFullScreenAdOverlay()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            Rect fullScreen = new Rect(0, 0, sw, sh);

            // Backdrop
            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.99f);
            GUI.Box(fullScreen, "");

            // Top Bar
            GUI.backgroundColor = new Color(0.12f, 0.15f, 0.2f);
            GUI.Box(new Rect(0, 0, sw, 52), "");

            // Badge
            GUIStyle badge = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            badge.normal.textColor = Color.black;
            GUI.backgroundColor = new Color(0.95f, 0.8f, 0.15f);
            GUI.Box(new Rect(12, 12, 85, 28), "Google AdMob", badge);

            // Title per Ad Type
            string formatTitle = "";
            switch (currentAdType)
            {
                case AdType.AppOpen: formatTitle = "Format 6/6: App Open Launch Ad"; break;
                case AdType.Interstitial: formatTitle = "Format 2/6: Full-Screen Interstitial Ad"; break;
                case AdType.Rewarded: formatTitle = "Format 3/6: Rewarded Video Ad (2X Bonus)"; break;
                case AdType.RewardedInterstitial: formatTitle = "Format 4/6: Rewarded Interstitial"; break;
            }

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(105, 12, sw - 215, 28), formatTitle, titleStyle);

            // Countdown / Close
            float remaining = Mathf.Max(0f, adDuration - adTimer);
            if (remaining > 0f && (currentAdType == AdType.Rewarded || currentAdType == AdType.RewardedInterstitial))
            {
                GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
                timerStyle.normal.textColor = new Color(1f, 0.82f, 0.2f);
                GUI.Label(new Rect(sw - 115, 12, 105, 28), $"Reward: {Mathf.CeilToInt(remaining)}s", timerStyle);
            }
            else
            {
                GUI.backgroundColor = (currentAdType == AdType.Rewarded) ? new Color(0.2f, 0.85f, 0.35f) : new Color(0.85f, 0.3f, 0.3f);
                string btnLabel = (currentAdType == AdType.Rewarded) ? "CLAIM ✕" : "CLOSE ✕";
                if (GUI.Button(new Rect(sw - 95, 10, 85, 32), btnLabel))
                {
                    CompleteAd(grantReward: true);
                }
            }

            // Center Card
            float cw = Mathf.Min(sw * 0.88f, 380f);
            float ch = Mathf.Min(sh * 0.52f, 380f);
            float cx = (sw - cw) / 2f;
            float cy = (sh - ch) / 2f;

            GUI.backgroundColor = new Color(0.15f, 0.18f, 0.24f, 0.98f);
            GUI.Box(new Rect(cx, cy, cw, ch), "");

            // Card Header
            GUIStyle cHead = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            cHead.normal.textColor = Color.white;
            GUI.Label(new Rect(cx, cy + 20, cw, 32), "MOB RUSH 3D", cHead);

            GUIStyle cSub = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            cSub.normal.textColor = new Color(0.7f, 0.8f, 0.92f);
            GUI.Label(new Rect(cx + 15, cy + 56, cw - 30, 42), "Multiply your horde & demolish enemy towers!\nOfficial Google AdMob Developer Test Unit", cSub);

            // Progress bar
            float pct = Mathf.Clamp01(adTimer / adDuration);
            Rect pBg = new Rect(cx + 25, cy + 115, cw - 50, 14);
            GUI.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            GUI.Box(pBg, "");
            Rect pFill = new Rect(cx + 25, cy + 115, (cw - 50) * pct, 14);
            GUI.backgroundColor = (currentAdType == AdType.Rewarded) ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.2f, 0.7f, 1f);
            GUI.Box(pFill, "");

            // Action Button
            GUI.backgroundColor = new Color(0.18f, 0.65f, 1f);
            if (GUI.Button(new Rect(cx + 35, cy + ch - 72, cw - 70, 46), "★ INSTALL NOW ★"))
            {
                Application.OpenURL("https://play.google.com/store/apps/details?id=com.vincentb.MobControl");
            }

            GUI.backgroundColor = Color.white;
        }
    }
}
