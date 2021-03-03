﻿setTimeout(function () {
    if (!WebSocket) {
        console.log("No WebSocket support available for LiveReload to work.");
        return;
    }

    var retry = 0;
    var isClosing = false;
    window.addEventListener("beforeunload", function () {
        // Prevent reload events triggered by closing the connection from interrupting navigation.
        isClosing = true;
        console.debug("Live Reload is paused due to beforeunload event.");
        setTimeout(function () {
            // Assume that the user clicked Stay on Page if this logic is executing after the timeout.
            isClosing = false;
            if (connection) {
                connection.onopen = null;
                connection = tryConnect(true);
                console.debug("Live Reload has resumed after unload was cancelled.");
            }
        }, 2500);
    });
    var connection = tryConnect(true);

    function tryConnect(retryOnFail) {
        try {
            var host = '{0}';
            connection = new WebSocket(host);
        }
        catch (ex) {
            console.log(ex);
            if (retryOnFail)
                retryConnection();
        }

        if (!connection)
            return null;


        connection.onmessage = function (message) {
            if (message.data == 'DelayRefresh') {
                console.log('Live Reload Delayed Reload.');
                setTimeout(
                    function () {
                        location.reload();
                    }, 500);
            }
            if (message.data == 'Refresh')
                setTimeout(function () { location.reload(); }, 10);
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
    function retryConnection() {
        var interval = setInterval(function () {
            console.log('Live Reload retrying connection.');
            connection.onopen = null;
            connection = tryConnect(false);
            if (connection) {
                if (connection.readyState === 1) {
                    location.reload(true);
                    clearInterval(interval);
                } else {
                    connection.onopen = function (event) {
                        console.log('Live Reload socket connected.');
                        location.reload(true);
                        clearInterval(interval);
                    }
                }
            }
        }, 500);
    }

}, 500);
