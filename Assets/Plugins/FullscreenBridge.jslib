mergeInto(LibraryManager.library, {
  WebGLSetFullscreen: function (enabled) {
    if (typeof window.unitySetFullscreen === "function") {
      window.unitySetFullscreen(enabled ? 1 : 0);
      return;
    }

    console.warn("unitySetFullscreen is not ready yet.");
  }
});
