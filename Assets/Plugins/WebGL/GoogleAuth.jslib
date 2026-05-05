mergeInto(LibraryManager.library, {

    GoogleAuth_Init: function (clientIdPtr) {
        var clientId = UTF8ToString(clientIdPtr);
        window.__googleClientId = clientId;
        window.__googleGoName   = null;
        window.__googleClient   = null;

        if (document.getElementById('gsi-script')) return;

        var script   = document.createElement('script');
        script.id    = 'gsi-script';
        script.src   = 'https://accounts.google.com/gsi/client';
        script.async = true;
        document.head.appendChild(script);

        // Pre-init token client saat library selesai load
        var attempts = 0;
        var interval = setInterval(function () {
            attempts++;
            if (typeof google !== 'undefined' && google.accounts && google.accounts.oauth2) {
                clearInterval(interval);
                window.__googleClient = google.accounts.oauth2.initTokenClient({
                    client_id    : window.__googleClientId,
                    scope        : 'openid email profile',
                    callback     : function (tokenResponse) {
                        var goName = window.__googleGoName;
                        if (!goName) return;
                        if (tokenResponse.error) {
                            SendMessage(goName, 'OnGoogleSignInFailed', tokenResponse.error);
                            return;
                        }
                        var data = JSON.stringify({ accessToken: tokenResponse.access_token });
                        SendMessage(goName, 'OnGoogleSignInSuccess', data);
                    },
                    error_callback: function (err) {
                        var goName = window.__googleGoName;
                        if (goName && err.type !== 'popup_closed') {
                            SendMessage(goName, 'OnGoogleSignInFailed', err.type || 'Unknown error');
                        }
                    }
                });
            } else if (attempts > 50) {
                clearInterval(interval);
            }
        }, 200);
    },

    GoogleAuth_SignIn: function (gameObjectNamePtr) {
        window.__googleGoName = UTF8ToString(gameObjectNamePtr);

        if (window.__googleClient) {
            window.__googleClient.requestAccessToken({ prompt: 'select_account' });
        } else {
            SendMessage(window.__googleGoName, 'OnGoogleSignInFailed', 'Google Sign-In not ready');
        }
    }

});
