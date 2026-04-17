var DerivWebSocketPlugin = {

    $webSockets: {},
    $nextId: 0,

    WS_Create: function(urlPtr, gameObjectNamePtr) {
        var url = UTF8ToString(urlPtr);
        var goName = UTF8ToString(gameObjectNamePtr);
        var id = webSockets.$nextId || 0;
        webSockets.$nextId = id + 1;

        if (!webSockets) webSockets = {};

        try {
            var ws = new WebSocket(url);
            webSockets[id] = { ws: ws, goName: goName };

            ws.onopen = function() {
                SendMessage(goName, 'OnWsOpen', id.toString());
            };

            ws.onmessage = function(evt) {
                // Prefix message with connection id so C# can route it
                SendMessage(goName, 'OnWsMessage', id.toString() + '|' + evt.data);
            };

            ws.onerror = function(evt) {
                SendMessage(goName, 'OnWsError', id.toString() + '|WebSocket error');
            };

            ws.onclose = function(evt) {
                SendMessage(goName, 'OnWsClose', id.toString() + '|' + evt.code + '|' + (evt.reason || ''));
                delete webSockets[id];
            };
        } catch (e) {
            console.error('[DerivWS] Failed to create WebSocket:', e);
            return -1;
        }

        return id;
    },

    WS_Send: function(id, msgPtr) {
        var msg = UTF8ToString(msgPtr);
        var entry = webSockets[id];
        if (entry && entry.ws && entry.ws.readyState === WebSocket.OPEN) {
            entry.ws.send(msg);
            return 1;
        }
        return 0;
    },

    WS_Close: function(id, code) {
        var entry = webSockets[id];
        if (entry && entry.ws) {
            try {
                entry.ws.close(code || 1000);
            } catch (e) {
                console.warn('[DerivWS] Error closing socket:', e);
            }
        }
    },

    WS_GetState: function(id) {
        var entry = webSockets[id];
        if (entry && entry.ws) {
            return entry.ws.readyState;
        }
        return 3; // CLOSED
    },

    // ==================== OAuth2 PKCE Functions ====================

    // Read a single query param from the current URL (e.g. "code" or "state")
    // Returns empty string if not found. Caller must free the returned string.
    OAuth_GetUrlParam: function(paramPtr) {
        var param = UTF8ToString(paramPtr);
        var url = new URL(window.location.href);
        var value = url.searchParams.get(param) || '';
        var bytes = lengthBytesUTF8(value) + 1;
        var buf = _malloc(bytes);
        stringToUTF8(value, buf, bytes);
        return buf;
    },

    // Redirect the browser to the OAuth authorization URL (same tab).
    OAuth_Redirect: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        console.log('[DerivAuth] Redirecting to:', url);
        window.location.href = url;
    },

    // Generate PKCE code_verifier + code_challenge using Web Crypto API.
    // Stores verifier in sessionStorage under key 'deriv_pkce_verifier'.
    // Stores state in sessionStorage under key 'deriv_oauth_state'.
    // Calls SendMessage(goName, 'OnPKCEReady', '<challenge>|<state>') when done.
    OAuth_GeneratePKCE: function(gameObjectNamePtr) {
        var goName = UTF8ToString(gameObjectNamePtr);

        // Generate random code_verifier (64 random bytes -> base64url)
        var array = new Uint8Array(64);
        crypto.getRandomValues(array);
        var verifier = btoa(String.fromCharCode.apply(null, array))
            .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

        // Generate random state
        var stateBytes = new Uint8Array(16);
        crypto.getRandomValues(stateBytes);
        var state = Array.from(stateBytes).map(function(b) {
            return b.toString(16).padStart(2, '0');
        }).join('');

        sessionStorage.setItem('deriv_pkce_verifier', verifier);
        sessionStorage.setItem('deriv_oauth_state', state);

        // Derive code_challenge = BASE64URL(SHA256(verifier))
        crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier)).then(function(hash) {
            var challenge = btoa(String.fromCharCode.apply(null, new Uint8Array(hash)))
                .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
            SendMessage(goName, 'OnPKCEReady', challenge + '|' + state);
        });
    },

    // Retrieve the stored code_verifier from sessionStorage.
    OAuth_GetVerifier: function() {
        var value = sessionStorage.getItem('deriv_pkce_verifier') || '';
        var bytes = lengthBytesUTF8(value) + 1;
        var buf = _malloc(bytes);
        stringToUTF8(value, buf, bytes);
        return buf;
    },

    // Clear OAuth code/state params from the URL bar without reloading the page.
    OAuth_ClearUrlParams: function() {
        sessionStorage.removeItem('deriv_pkce_verifier');
        sessionStorage.removeItem('deriv_oauth_state');
        window.history.replaceState({}, document.title, window.location.pathname);
    }
};

autoAddDeps(DerivWebSocketPlugin, '$webSockets');
autoAddDeps(DerivWebSocketPlugin, '$nextId');
mergeInto(LibraryManager.library, DerivWebSocketPlugin);
