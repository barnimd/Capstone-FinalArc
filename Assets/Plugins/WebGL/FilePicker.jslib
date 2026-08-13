mergeInto(LibraryManager.library, {

    // Opens a native file dialog, then resizes and re-encodes the picked image
    // BEFORE handing it to Unity.
    //
    // The resize happens here rather than in C# for two reasons. A phone photo is
    // 4-8 MB; passing that through SendMessage means marshalling a ~10 MB string,
    // and decoding it into a Texture2D would cost 30-50 MB of the fixed WebGL heap
    // for an image that ends up displayed at roughly 800px. Doing it on a canvas
    // keeps both out of Unity entirely — what crosses the boundary is ~250 KB.
    // quality is 0-100, kept an integer so nothing depends on float marshalling.
    FilePicker_Open: function (goNamePtr, maxWidth, quality) {
        var goName = UTF8ToString(goNamePtr);
        var q = Math.min(1, Math.max(0.1, quality / 100));

        var input  = document.createElement('input');
        input.type   = 'file';
        input.accept = 'image/png,image/jpeg,image/webp';
        input.style.display = 'none';
        document.body.appendChild(input);

        var cleanup = function () {
            if (input.parentNode) input.parentNode.removeChild(input);
        };

        input.onchange = function (e) {
            var file = e.target.files && e.target.files[0];
            if (!file) {
                SendMessage(goName, 'OnFilePickCancelled', '');
                cleanup();
                return;
            }

            var reader = new FileReader();

            reader.onload = function () {
                var img = new Image();

                img.onload = function () {
                    var scale = Math.min(1, maxWidth / img.width);
                    var w = Math.max(1, Math.round(img.width  * scale));
                    var h = Math.max(1, Math.round(img.height * scale));

                    var canvas = document.createElement('canvas');
                    canvas.width  = w;
                    canvas.height = h;

                    var ctx = canvas.getContext('2d');
                    // JPEG has no alpha. Without this, a transparent PNG would come
                    // out with a black background instead of white.
                    ctx.fillStyle = '#ffffff';
                    ctx.fillRect(0, 0, w, h);
                    ctx.drawImage(img, 0, 0, w, h);

                    var dataUrl;
                    try {
                        dataUrl = canvas.toDataURL('image/jpeg', q);
                    } catch (err) {
                        SendMessage(goName, 'OnFilePickFailed', 'encode failed: ' + err.message);
                        cleanup();
                        return;
                    }

                    SendMessage(goName, 'OnFilePicked', dataUrl);
                    cleanup();
                };

                img.onerror = function () {
                    SendMessage(goName, 'OnFilePickFailed', 'File bukan gambar yang valid');
                    cleanup();
                };

                img.src = reader.result;
            };

            reader.onerror = function () {
                SendMessage(goName, 'OnFilePickFailed', 'Gagal membaca file');
                cleanup();
            };

            reader.readAsDataURL(file);
        };

        // Safari and Firefox fire nothing at all when the dialog is dismissed, so
        // there is deliberately no cancel timeout here — a stray "cancelled" would
        // be worse than a picker the user simply reopens.
        input.click();
    }

});
