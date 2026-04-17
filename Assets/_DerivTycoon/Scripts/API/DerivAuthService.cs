using System;
using System.Collections;
using System.Text;
using DerivTycoon.API.Models;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DerivTycoon.API
{
    public class DerivAuthService : MonoBehaviour
    {
        public static DerivAuthService Instance { get; private set; }

        [Header("OAuth2 Settings")]
        [SerializeField] public string ClientId;
        [SerializeField] public string AppId = "1089";
        [SerializeField] public string RedirectUri;
        [SerializeField] public string TokenExchangeUrl; // e.g. https://your-app.vercel.app/api/auth/token

        [Header("Editor Testing Only ??? DO NOT COMMIT")]
        [SerializeField] public string EditorTestToken;

        public bool IsAuthenticated { get; private set; }

        public event Action<string> OnTradingWsReady;
        public event Action OnAuthRequired; // fire to show login button

        private const string BaseUrl = "https://api.derivws.com";
        private const string AuthUrl = "https://auth.deriv.com/oauth2/auth";
        private string _accessToken;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string OAuth_GetUrlParam(string param);
        [DllImport("__Internal")] private static extern void   OAuth_Redirect(string url);
        [DllImport("__Internal")] private static extern void   OAuth_GeneratePKCE(string gameObjectName);
        [DllImport("__Internal")] private static extern string OAuth_GetVerifier();
        [DllImport("__Internal")] private static extern void   OAuth_ClearUrlParams();
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
#if UNITY_EDITOR
            // Editor: use manual test token if set
            if (!string.IsNullOrEmpty(EditorTestToken))
            {
                Debug.Log("[DerivAuth] Editor mode: using test token");
                _accessToken = EditorTestToken;
                StartCoroutine(ConnectTradingWs());
            }
            else
            {
                Debug.Log("[DerivAuth] Editor mode: no test token ??? demo mode");
                OnAuthRequired?.Invoke();
            }
#elif UNITY_WEBGL
            // WebGL: use Application.absoluteURL to read OAuth callback params (more reliable than jslib)
            string absoluteUrl = Application.absoluteURL;
            Debug.Log($"[DerivAuth] Start() absoluteURL={absoluteUrl.Substring(0, Mathf.Min(80, absoluteUrl.Length))}");

            string code  = GetQueryParam(absoluteUrl, "code");
            string state = GetQueryParam(absoluteUrl, "state");
            Debug.Log($"[DerivAuth] code='{(string.IsNullOrEmpty(code) ? "EMPTY" : code.Substring(0, Mathf.Min(20, code.Length)) + "...")}' state='{(string.IsNullOrEmpty(state) ? "EMPTY" : "found")}'");

            if (!string.IsNullOrEmpty(code))
            {
                Debug.Log("[DerivAuth] OAuth code detected — exchanging for token...");
                StartCoroutine(ExchangeCodeForToken(code, state));
            }
            else
            {
                Debug.Log("[DerivAuth] No OAuth code in URL — showing login");
                OnAuthRequired?.Invoke();
            }
#else
            OnAuthRequired?.Invoke();
#endif
        }

        private static string GetQueryParam(string url, string key)
        {
            int qIndex = url.IndexOf('?');
            if (qIndex < 0) return "";
            string query = url.Substring(qIndex + 1);
            foreach (string part in query.Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0 && part.Substring(0, eq) == key)
                    return Uri.UnescapeDataString(part.Substring(eq + 1));
            }
            return "";
        }

        // Called by jslib after PKCE generation completes
        // challenge|state
        public void OnPKCEReady(string data)
        {
            var parts = data.Split('|');
            if (parts.Length < 2) return;
            string challenge = parts[0];
            string state = parts[1];

            string authUrl = $"{AuthUrl}" +
                $"?response_type=code" +
                $"&client_id={Uri.EscapeDataString(ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                $"&scope=trade" +
                $"&state={state}" +
                $"&code_challenge={challenge}" +
                $"&code_challenge_method=S256";

            Debug.Log($"[DerivAuth] Redirecting to Deriv OAuth...");

#if UNITY_WEBGL && !UNITY_EDITOR
            OAuth_Redirect(authUrl);
#else
            Debug.Log($"[DerivAuth] (Editor) Would redirect to: {authUrl}");
#endif
        }

        // Called by "Login with Deriv" button
        public void StartOAuthFlow()
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                Debug.LogError("[DerivAuth] ClientId not set ??? cannot start OAuth flow");
                return;
            }

            Debug.Log("[DerivAuth] Starting PKCE generation...");

#if UNITY_WEBGL && !UNITY_EDITOR
            OAuth_GeneratePKCE(gameObject.name);
            // Flow continues in OnPKCEReady callback
#else
            Debug.LogWarning("[DerivAuth] OAuth redirect not available in Editor");
#endif
        }

        private IEnumerator ExchangeCodeForToken(string code, string returnedState)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string verifier = OAuth_GetVerifier();
            Debug.Log($"[DerivAuth] verifier='{(string.IsNullOrEmpty(verifier) ? "EMPTY" : "found")}' url='{TokenExchangeUrl}'");
            OAuth_ClearUrlParams();
#else
            string verifier = "";
#endif
            if (string.IsNullOrEmpty(verifier))
            {
                Debug.LogError("[DerivAuth] No PKCE verifier in localStorage — auth failed");
                OnAuthRequired?.Invoke();
                yield break;
            }

            if (string.IsNullOrEmpty(TokenExchangeUrl))
            {
                Debug.LogError("[DerivAuth] TokenExchangeUrl not set");
                OnAuthRequired?.Invoke();
                yield break;
            }

            var body = new
            {
                code,
                code_verifier = verifier,
                redirect_uri = RedirectUri,
                client_id = ClientId
            };

            string json = $"{{\"code\":\"{code}\",\"code_verifier\":\"{verifier}\"," +
                          $"\"redirect_uri\":\"{RedirectUri}\",\"client_id\":\"{ClientId}\"}}";

            using var request = new UnityWebRequest(TokenExchangeUrl, "POST");
            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DerivAuth] Token exchange failed: {request.error}\n{request.downloadHandler.text}");
                OnAuthRequired?.Invoke();
                yield break;
            }

            Debug.Log($"[DerivAuth] Token exchange response: {request.downloadHandler.text}");

            var tokenResp = JsonUtility.FromJson<TokenExchangeResponse>(request.downloadHandler.text);
            if (string.IsNullOrEmpty(tokenResp?.access_token))
            {
                Debug.LogError("[DerivAuth] No access_token in response");
                OnAuthRequired?.Invoke();
                yield break;
            }

            _accessToken = tokenResp.access_token;
            IsAuthenticated = true;
            Debug.Log("[DerivAuth] Token obtained ??? proceeding with trading auth");
            StartCoroutine(ConnectTradingWs());
        }

        private IEnumerator ConnectTradingWs()
        {
            // Step 1: GET accounts
            string accountId = null;
            yield return StartCoroutine(GetDemoAccountId(result => accountId = result));

            if (string.IsNullOrEmpty(accountId))
            {
                Debug.LogError("[DerivAuth] Could not find demo account");
                yield break;
            }

            Debug.Log($"[DerivAuth] Demo account: {accountId}");

            // Step 2: POST OTP
            string wsUrl = null;
            yield return StartCoroutine(GetOtp(accountId, result => wsUrl = result));

            if (string.IsNullOrEmpty(wsUrl))
            {
                Debug.LogError("[DerivAuth] Could not get OTP WS URL");
                yield break;
            }

            Debug.Log("[DerivAuth] Trading WS ready");
            OnTradingWsReady?.Invoke(wsUrl);
        }

        private IEnumerator GetDemoAccountId(Action<string> callback)
        {
            using var request = UnityWebRequest.Get($"{BaseUrl}/trading/v1/options/accounts");
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
            if (!string.IsNullOrEmpty(AppId))
                request.SetRequestHeader("Deriv-App-ID", AppId);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DerivAuth] GET accounts failed: {request.error} ({request.responseCode})\n{request.downloadHandler.text}");
                callback(null);
                yield break;
            }

            Debug.Log($"[DerivAuth] Accounts: {request.downloadHandler.text}");

            var response = JsonUtility.FromJson<AccountsRestResponse>(request.downloadHandler.text);
            if (response?.data == null || response.data.Length == 0)
            {
                callback(null);
                yield break;
            }

            foreach (var account in response.data)
            {
                if (account.account_type == "demo")
                {
                    callback(account.account_id);
                    yield break;
                }
            }

            Debug.LogWarning("[DerivAuth] No demo account found, using first");
            callback(response.data[0].account_id);
        }

        private IEnumerator GetOtp(string accountId, Action<string> callback)
        {
            string url = $"{BaseUrl}/trading/v1/options/accounts/{accountId}/otp";
            using var request = new UnityWebRequest(url, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
            if (!string.IsNullOrEmpty(AppId))
                request.SetRequestHeader("Deriv-App-ID", AppId);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DerivAuth] POST otp failed: {request.error} ({request.responseCode})\n{request.downloadHandler.text}");
                callback(null);
                yield break;
            }

            var response = JsonUtility.FromJson<OtpRestResponse>(request.downloadHandler.text);
            callback(response?.data?.url);
        }
    }

    [Serializable]
    public class TokenExchangeResponse
    {
        public string access_token;
        public int expires_in;
    }
}
