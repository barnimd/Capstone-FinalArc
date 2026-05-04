mergeInto(LibraryManager.library, {

    GoogleAuth_Init: function (clientIdPtr) {
        var clientId = UTF8ToString(clientIdPtr);
        window.__googleClientId = clientId;

        if (document.getElementById('gsi-script')) return;

        var script    = document.createElement('script');
        script.id     = 'gsi-script';
        script.src    = 'https://accounts.google.com/gsi/client';
        script.async  = true;
        document.head.appendChild(script);
    },

    GoogleAuth_SignIn: function (gameObjectNamePtr) {
        var goName = UTF8ToString(gameObjectNamePtr);

        function run() {
            google.accounts.id.initialize({
                client_id            : window.__googleClientId,
                callback             : function (response) {
                    if (!response.credential) {
                        SendMessage(goName, 'OnGoogleSignInFailed', 'No credential received');
                        return;
                    }
                    try {
                        var parts   = response.credential.split('.');
                        var payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
                        var data    = JSON.stringify({
                            idToken : response.credential,
                            email   : payload.email || '',
                            name    : payload.name  || ''
                        });
                        SendMessage(goName, 'OnGoogleSignInSuccess', data);
                    } catch (e) {
                        SendMessage(goName, 'OnGoogleSignInFailed', 'Parse error: ' + e.message);
                    }
                },
                ux_mode              : 'popup',
                auto_select          : false,
                cancel_on_tap_outside: true
            });

            google.accounts.id.prompt(function (notification) {
                if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
                    SendMessage(goName, 'OnGoogleSignInFailed', 'Sign-in dismissed');
                }
            });
        }

        if (typeof google !== 'undefined' && google.accounts && google.accounts.id) {
            run();
        } else {
            var attempts = 0;
            var interval = setInterval(function () {
                attempts++;
                if (typeof google !== 'undefined' && google.accounts && google.accounts.id) {
                    clearInterval(interval);
                    run();
                } else if (attempts > 50) {
                    clearInterval(interval);
                    SendMessage(goName, 'OnGoogleSignInFailed', 'Google library failed to load');
                }
            }, 200);
        }
    }

});
