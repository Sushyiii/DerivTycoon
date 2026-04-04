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
    }
};

autoAddDeps(DerivWebSocketPlugin, '$webSockets');
autoAddDeps(DerivWebSocketPlugin, '$nextId');
mergeInto(LibraryManager.library, DerivWebSocketPlugin);
