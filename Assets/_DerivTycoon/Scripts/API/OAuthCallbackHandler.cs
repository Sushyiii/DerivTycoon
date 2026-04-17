using UnityEngine;

namespace DerivTycoon.API
{
    public static class OAuthCallbackHandler
    {
        private static string _pendingCode;
        private static string _pendingState;

        public static string PendingCode  => _pendingCode;
        public static string PendingState => _pendingState;
        public static bool HasPendingCode => !string.IsNullOrEmpty(_pendingCode);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DetectOAuthCallback()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string url = Application.absoluteURL ?? string.Empty;
            Debug.Log($"[DerivAuth] RuntimeInit URL={url}");

            _pendingCode  = GetQueryParam(url, "code");
            _pendingState = GetQueryParam(url, "state");

            if (!string.IsNullOrEmpty(_pendingCode))
                Debug.Log($"[DerivAuth] RuntimeInit: OAuth code detected ??? length={_pendingCode.Length}");
            else
                Debug.Log("[DerivAuth] RuntimeInit: no OAuth code in URL");
#endif
        }

        public static void ClearPending()
        {
            _pendingCode  = null;
            _pendingState = null;
        }

        private static string GetQueryParam(string url, string key)
        {
            int q = url.IndexOf('?');
            if (q < 0) return "";
            string query = url.Substring(q + 1);
            foreach (string part in query.Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0 && part.Substring(0, eq) == key)
                    return System.Uri.UnescapeDataString(part.Substring(eq + 1));
            }
            return "";
        }
    }
}
