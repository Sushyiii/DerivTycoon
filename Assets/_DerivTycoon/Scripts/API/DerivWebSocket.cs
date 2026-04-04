using System;
using System.Collections.Concurrent;
using UnityEngine;

#if !UNITY_WEBGL || UNITY_EDITOR
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#else
using System.Runtime.InteropServices;
#endif

namespace DerivTycoon.API
{
    public class DerivWebSocket : MonoBehaviour
    {
        public event Action OnConnected;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnError;
        public event Action<int, string> OnDisconnected;

        public bool IsConnected { get; private set; }

        private int _connectionId = -1;
        private string _url;
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int WS_Create(string url, string gameObjectName);
        [DllImport("__Internal")] private static extern int WS_Send(int id, string message);
        [DllImport("__Internal")] private static extern void WS_Close(int id, int code);
        [DllImport("__Internal")] private static extern int WS_GetState(int id);
#else
        private ClientWebSocket _nativeSocket;
        private CancellationTokenSource _cts;
#endif

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public void Connect(string url)
        {
            _url = url;
            Debug.Log($"[DerivWS] Connecting to {url}");

#if UNITY_WEBGL && !UNITY_EDITOR
            _connectionId = WS_Create(url, gameObject.name);
            if (_connectionId < 0)
            {
                Debug.LogError("[DerivWS] Failed to create WebSocket connection");
            }
#else
            ConnectNativeAsync();
#endif
        }

        public void Send(string message)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[DerivWS] Cannot send, not connected");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            WS_Send(_connectionId, message);
#else
            SendNativeAsync(message);
#endif
        }

        public void Close()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_connectionId >= 0)
            {
                WS_Close(_connectionId, 1000);
                _connectionId = -1;
            }
#else
            CloseNativeAsync();
#endif
            IsConnected = false;
        }

        private void OnDestroy()
        {
            Close();
        }

        // ==================== WebGL callbacks (called from jslib via SendMessage) ====================

        public void OnWsOpen(string idStr)
        {
            _mainThreadActions.Enqueue(() =>
            {
                IsConnected = true;
                Debug.Log($"[DerivWS] Connected (id={idStr})");
                OnConnected?.Invoke();
            });
        }

        public void OnWsMessage(string data)
        {
            // data format: "id|jsonPayload"
            int sep = data.IndexOf('|');
            if (sep < 0) return;
            string json = data.Substring(sep + 1);

            _mainThreadActions.Enqueue(() =>
            {
                OnMessageReceived?.Invoke(json);
            });
        }

        public void OnWsError(string data)
        {
            int sep = data.IndexOf('|');
            string error = sep >= 0 ? data.Substring(sep + 1) : data;

            _mainThreadActions.Enqueue(() =>
            {
                Debug.LogError($"[DerivWS] Error: {error}");
                OnError?.Invoke(error);
            });
        }

        public void OnWsClose(string data)
        {
            // data format: "id|code|reason"
            string[] parts = data.Split('|');
            int code = parts.Length > 1 && int.TryParse(parts[1], out int c) ? c : 0;
            string reason = parts.Length > 2 ? parts[2] : "";

            _mainThreadActions.Enqueue(() =>
            {
                IsConnected = false;
                Debug.Log($"[DerivWS] Disconnected (code={code}, reason={reason})");
                OnDisconnected?.Invoke(code, reason);
            });
        }

        // ==================== Native WebSocket (Editor / Standalone) ====================

#if !UNITY_WEBGL || UNITY_EDITOR
        private async void ConnectNativeAsync()
        {
            _nativeSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                await _nativeSocket.ConnectAsync(new Uri(_url), _cts.Token);
                _mainThreadActions.Enqueue(() =>
                {
                    IsConnected = true;
                    Debug.Log("[DerivWS] Connected (native)");
                    OnConnected?.Invoke();
                });
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                _mainThreadActions.Enqueue(() =>
                {
                    Debug.LogError($"[DerivWS] Connection failed: {ex.Message}");
                    OnError?.Invoke(ex.Message);
                });
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();

            try
            {
                while (_nativeSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = await _nativeSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        int code = (int)(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure);
                        string reason = result.CloseStatusDescription ?? "";
                        _mainThreadActions.Enqueue(() =>
                        {
                            IsConnected = false;
                            OnDisconnected?.Invoke(code, reason);
                        });
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        string message = sb.ToString();
                        sb.Clear();
                        _mainThreadActions.Enqueue(() =>
                        {
                            OnMessageReceived?.Invoke(message);
                        });
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _mainThreadActions.Enqueue(() =>
                {
                    Debug.LogError($"[DerivWS] Receive error: {ex.Message}");
                    OnError?.Invoke(ex.Message);
                    IsConnected = false;
                });
            }
        }

        private async void SendNativeAsync(string message)
        {
            if (_nativeSocket?.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(message);
            try
            {
                await _nativeSocket.SendAsync(
                    new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            }
            catch (Exception ex)
            {
                _mainThreadActions.Enqueue(() =>
                {
                    Debug.LogError($"[DerivWS] Send error: {ex.Message}");
                    OnError?.Invoke(ex.Message);
                });
            }
        }

        private async void CloseNativeAsync()
        {
            if (_nativeSocket == null) return;

            try
            {
                _cts?.Cancel();
                if (_nativeSocket.State == WebSocketState.Open)
                {
                    await _nativeSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
            }
            catch (Exception) { }
            finally
            {
                _nativeSocket?.Dispose();
                _nativeSocket = null;
                _cts?.Dispose();
                _cts = null;
            }
        }
#endif
    }
}
