setTimeout(function () {
    if (!WebSocket) {
        console.log("No WebSocket support available for LiveReload to work.");
        return;
    }

    var retry = 0;
    var isClosing = false;
    var connection = tryConnect(true);

    function tryConnect(retryOnFail) {
        try {
            var host = '{0}';
            connection = new WebSocket(host);
        }
        catch (ex) {
            console.log("Failed Live Reload connection",ex);
            if (retryOnFail)
                retryConnection();
        }

        if (!connection)
            return null;


        connection.onmessage = function (message) {
            if (message.data == 'DelayRefresh') {
                console.log('Live Reload Delayed Reload.');
                setTimeout(reloadUrl, 1000);
            }
            else if (message.data == 'Refresh') {            
                console.log('Live Reload Page reloading.');
                setTimeout(reloadUrl, 30);            
            }
        }
        connection.onerror = function (event) {
            console.log('Live Reload Socket error.', event);
            if (retryOnFail)
                retryConnection();
        }
        connection.onclose = function (event) {
            console.log('Live Reload Socket closed.');
            if (retryOnFail && !isClosing)
                retryConnection();
        }
        connection.onopen = function (event) {
            console.log('Live Reload socket connected.');
        }
        return connection;
    }
    function reloadUrl() {
        window.location.reload();
    }
    function retryConnection() {
        var interval = setInterval(function () {
            console.log('Live Reload retrying connection.');
            connection.onopen = null;
            connection = tryConnect(false);
            if (connection) {
                if (connection.readyState === 1) {
                    reloadUrl();
                    clearInterval(interval);
                } else {
                    connection.onopen = function (event) {
                        console.log('Live Reload socket connected.');
                        reloadUrl();
                        clearInterval(interval);
                    }
                }
            }
        }, 500);
    }

    // Prevent reload events triggered by closing the connection from interrupting navigation.
    window.addEventListener("beforeunload", function () {
        isClosing = true;
        console.log("Live Reload paused for page unload.");
        setTimeout(function () {
            // Assume that the user clicked Stay on Page if this logic is executing after the timeout.
            isClosing = false;
            if (connection) {
                connection.onopen = null;
                connection = tryConnect(true);
                console.log("Live Reload resumed after unload was cancelled.");
            }
        }, 2500);
    });
}, 500);
