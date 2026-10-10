mergeInto(LibraryManager.library, {
    MadSlimeConfirmSave: function (targetPointer, requestId, jsonPointer) {
        const target = UTF8ToString(targetPointer);
        const json = UTF8ToString(jsonPointer);
        const confirmed = function () {
            SendMessage(target, "OnSaveConfirmed", String(requestId));
        };
        const failed = function (error) {
            let reason = "Cloud storage rejected the write";
            if (error && error.message) {
                reason = error.message;
            }
            SendMessage(target, "OnSaveFailed", String(requestId) + "|" + reason);
        };
        const write = function () {
            if (typeof player === "undefined" || player === null) {
                throw new Error("Yandex player is unavailable");
            }
            const promise = player.setData({ saves: [json] }, true);
            if (promise === null || typeof promise === "undefined" || typeof promise.then !== "function") {
                throw new Error("Cloud storage did not return a confirmation promise");
            }
            return promise;
        };
        if (typeof Module["madSlimeSaveQueue"] === "undefined") {
            Module["madSlimeSaveQueue"] = Promise.resolve();
        }
        Module["madSlimeSaveQueue"] = Module["madSlimeSaveQueue"].then(write).then(confirmed, failed);
    }
});
