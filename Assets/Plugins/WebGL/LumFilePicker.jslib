// LumFilePicker.jslib
//
// Self-contained WebGL file upload / download bridge.
//
// This restores the working parts of gkngkc/UnityStandaloneFileBrowser's
// StandaloneFileBrowser.jslib, which the Netherlands3D fork gutted:
//   - the fork's UploadFile.onchange calls ReadFiles([]) with an EMPTY array,
//     never touches event.target.files, and never SendMessage()s the caller.
//
// Every symbol here is prefixed "LumFilePicker_" so it CANNOT collide with the
// symbols already defined by the Netherlands3D jslibs, which stay in the
// project untouched:
//   StandaloneFileBrowser.jslib : InitializeIndexedDB, BrowseForFile,
//                                 UploadFromIndexedDB, DownloadFromIndexedDB,
//                                 AddFileInput, SyncFilesFrom/ToIndexedDB,
//                                 ClearFileInputFields, UploadFile, DownloadFile
//   Interface.jslib             : DisplayDOMObjectWithID, ChangeInterfaceScale
// Duplicate keys passed to mergeInto silently overwrite each other, so the
// prefix is load-bearing, not cosmetic.
//
// USER ACTIVATION
// ---------------
// input.click() is NOT called immediately. Unity processes pointer events one
// frame after the real DOM event, which puts a direct .click() outside the
// gesture task (Safari blocks this). Instead we arm document.onmouseup, so the
// click happens inside a genuine, still-pending user gesture.
//
// The corollary: this requires a real mouseup. A gamepad produces none, so
// LumFilePicker_Open can never open a dialog for a controller-only player.
// That is a browser constraint, not a bug in this file.

mergeInto(LibraryManager.library, {

    // Arms a hidden <input type=file>. The dialog opens on the next real mouseup.
    //
    // goPtr        : GameObject name for SendMessage (must be unique in scene)
    // methodPtr    : method on that GameObject, receives a string
    // filterPtr    : accept attribute, e.g. ".lummm" or ".png,.jpg" or "*"
    // multiselect  : allow multiple files
    //
    // The callback receives newline-separated blob URLs, or "" for
    // cancel / no selection.
    LumFilePicker_Open: function (goPtr, methodPtr, filterPtr, multiselect) {
        var gameObjectName = UTF8ToString(goPtr);
        var methodName = UTF8ToString(methodPtr);
        var filter = UTF8ToString(filterPtr);

        // Tear down any previous attempt so a stale input can never fire.
        if (window.__lumPicker && window.__lumPicker.settle) {
            window.__lumPicker.settle("");
        }

        var previous = document.getElementById("lum_file_picker_input");
        if (previous && previous.parentNode) {
            previous.parentNode.removeChild(previous);
        }

        var input = document.createElement("input");
        input.setAttribute("id", "lum_file_picker_input");
        input.setAttribute("type", "file");
        // Off-screen rather than display:none — some browsers refuse to open a
        // picker for an input that is not rendered at all.
        input.setAttribute(
            "style",
            "position:fixed;left:-9999px;top:-9999px;width:1px;height:1px;opacity:0;"
        );

        if (multiselect) {
            input.setAttribute("multiple", "");
        }

        // An unknown extension in `accept` makes some mobile browsers show an
        // empty file list. Pass "*" from C# if you hit that.
        if (filter && filter.length > 0 && filter !== "*") {
            input.setAttribute("accept", filter);
        }

        document.body.appendChild(input);

        var settled = false;

        function settle(payload) {
            if (settled) return;
            settled = true;

            window.removeEventListener("focus", onWindowFocus, true);
            document.onmouseup = null;

            if (input.parentNode) {
                input.parentNode.removeChild(input);
            }

            window.__lumPicker = null;

            try {
                SendMessage(gameObjectName, methodName, payload);
            } catch (e) {
                console.error("LumFilePicker: SendMessage failed", e);
            }
        }

        // Exposed so LumFilePicker_Cancel (and a C# watchdog) can force-resolve.
        window.__lumPicker = { settle: settle };

        input.onclick = function () {
            // Reset so re-picking the same file still fires change.
            this.value = null;

            // .click() fired, so the browser is about to show the dialog. Tell
            // C# to stand its watchdog down: from here the wait is the player
            // browsing folders, which can take minutes, and must NOT time out.
            try {
                SendMessage(gameObjectName, "OnBrowserDialogOpened");
            } catch (e) {
                console.error("LumFilePicker: SendMessage failed", e);
            }
        };

        input.onchange = function () {
            var files = this.files;

            if (!files || files.length === 0) {
                settle("");
                return;
            }

            var urls = [];
            for (var i = 0; i < files.length; i++) {
                urls.push(URL.createObjectURL(files[i]));
            }

            settle(urls.join("\n"));
        };

        // Modern browsers (Chrome 113+, Firefox 109+, Safari 16.4+) fire this
        // when the user dismisses the dialog. Without it, cancelling leaves the
        // C# side waiting on a callback that never arrives.
        input.addEventListener("cancel", function () {
            settle("");
        });

        // Fallback for older browsers with no `cancel` event: the window regains
        // focus when the dialog closes either way, so wait long enough for
        // `change` to win the race if a file was actually chosen.
        function onWindowFocus() {
            setTimeout(function () {
                settle("");
            }, 1500);
        }
        window.addEventListener("focus", onWindowFocus, { capture: true, once: true });

        // The actual gesture hook. See the header comment.
        document.onmouseup = function () {
            document.onmouseup = null;
            input.click();
        };
    },

    // Force-resolves a pending Open as cancelled. Called by the C# watchdog when
    // no mouseup ever arrives (gamepad, or the player navigated away mid-flow),
    // which would otherwise leave the picker armed forever.
    LumFilePicker_Cancel: function () {
        document.onmouseup = null;

        if (window.__lumPicker && window.__lumPicker.settle) {
            window.__lumPicker.settle("");
        }
    },

    // Triggers a browser download. There is no Save As dialog in a browser and
    // no asset can add one — the file lands in the download folder.
    //
    // Deliberately NOT deferred to document.onmouseup like the upload path:
    // anchor downloads do not require transient activation, and deferring would
    // mean a gamepad-triggered export never downloads at all.
    LumFilePicker_Download: function (goPtr, methodPtr, filenamePtr, byteArray, byteArraySize) {
        var gameObjectName = UTF8ToString(goPtr);
        var methodName = UTF8ToString(methodPtr);
        var filename = UTF8ToString(filenamePtr);

        // .slice() copies out of the Unity heap; the heap can be resized or
        // reused before the async download reads the blob.
        var bytes = new Uint8Array(HEAPU8.buffer, byteArray, byteArraySize).slice();
        var blob = new Blob([bytes], { type: "application/octet-stream" });
        var url = URL.createObjectURL(blob);

        var anchor = document.createElement("a");
        anchor.style.display = "none";
        anchor.href = url;
        anchor.download = filename;
        document.body.appendChild(anchor);
        anchor.click();

        setTimeout(function () {
            URL.revokeObjectURL(url);

            if (anchor.parentNode) {
                anchor.parentNode.removeChild(anchor);
            }

            try {
                SendMessage(gameObjectName, methodName, filename);
            } catch (e) {
                console.error("LumFilePicker: SendMessage failed", e);
            }
        }, 100);
    },

    // Blob URLs leak until revoked. C# calls this once it has read the bytes.
    LumFilePicker_RevokeUrl: function (urlPtr) {
        var url = UTF8ToString(urlPtr);

        try {
            URL.revokeObjectURL(url);
        } catch (e) {
            console.warn("LumFilePicker: could not revoke " + url, e);
        }
    }
});
