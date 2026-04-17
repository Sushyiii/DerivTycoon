using DerivTycoon.API;
using DerivTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DerivTycoon.UI
{
    public class LoginPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button demoButton;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Text statusText;

        private void Awake()
        {
            loginButton?.onClick.AddListener(OnLoginClicked);
            demoButton?.onClick.AddListener(OnDemoClicked);
        }

        private void OnEnable()
        {
            EventBus.OnGameStateChanged += OnGameStateChanged;
            if (DerivAuthService.Instance != null)
                DerivAuthService.Instance.OnAuthRequired += ShowLogin;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= OnGameStateChanged;
            if (DerivAuthService.Instance != null)
                DerivAuthService.Instance.OnAuthRequired -= ShowLogin;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                // If already authenticated, go straight to game
                bool authenticated = DerivAuthService.Instance != null && DerivAuthService.Instance.IsAuthenticated;
                if (!authenticated)
                    ShowLogin();
            }
            else
            {
                HideAll();
            }
        }

        private void ShowLogin()
        {
            loginPanel?.SetActive(true);
            loadingPanel?.SetActive(false);
            if (statusText != null) statusText.text = "";
        }

        private void HideAll()
        {
            loginPanel?.SetActive(false);
            loadingPanel?.SetActive(false);
        }

        private void OnLoginClicked()
        {
            var auth = DerivAuthService.Instance;
            if (auth == null) return;

            if (string.IsNullOrEmpty(auth.ClientId))
            {
                if (statusText != null) statusText.text = "App not configured for login.";
                return;
            }

            loginPanel?.SetActive(false);
            loadingPanel?.SetActive(true);
            if (statusText != null) statusText.text = "Redirecting to Deriv login...";

            auth.StartOAuthFlow();
        }

        private void OnDemoClicked()
        {
            HideAll();
            GameManager.Instance?.StartDemoMode();
        }
    }
}
