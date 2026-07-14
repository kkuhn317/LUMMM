(() => {
    "use strict";

    const CONTAINER_SELECTOR = "#unity-container";
    const CANVAS_SELECTOR = "#unity-canvas";

    let unityContainer = null;
    let unityCanvas = null;
    let resizeObserver = null;
    let scheduledFrame = null;

    function findUnityElements() {
        unityContainer =
            document.querySelector(CONTAINER_SELECTOR) ||
            document.querySelector(".unity-container");

        unityCanvas =
            document.querySelector(CANVAS_SELECTOR) ||
            document.querySelector("canvas");

        return unityContainer !== null && unityCanvas !== null;
    }

    function isFullscreen() {
        return Boolean(
            document.fullscreenElement ||
            document.webkitFullscreenElement
        );
    }

    function applyCanvasSize() {
        if (!unityContainer || !unityCanvas) {
            if (!findUnityElements()) {
                return;
            }
        }

        if (isFullscreen()) {
            unityContainer.style.width = "100vw";
            unityContainer.style.height = "100vh";
            unityContainer.style.maxWidth = "none";
            unityContainer.style.maxHeight = "none";

            unityCanvas.style.width = "100vw";
            unityCanvas.style.height = "100vh";
        } else {
            unityContainer.style.width = "100%";
            unityContainer.style.height = "100%";

            const rect = unityContainer.getBoundingClientRect();

            if (rect.width > 0 && rect.height > 0) {
                unityCanvas.style.width = `${Math.round(rect.width)}px`;
                unityCanvas.style.height = `${Math.round(rect.height)}px`;
            } else {
                unityCanvas.style.width = "100%";
                unityCanvas.style.height = "100%";
            }
        }

        unityContainer.style.boxSizing = "border-box";
        unityCanvas.style.boxSizing = "border-box";
        unityCanvas.style.display = "block";
    }

    function scheduleResize() {
        if (scheduledFrame !== null) {
            cancelAnimationFrame(scheduledFrame);
        }

        scheduledFrame = requestAnimationFrame(() => {
            scheduledFrame = null;
            applyCanvasSize();

            // Wait another frame for the browser's fullscreen layout to settle.
            requestAnimationFrame(applyCanvasSize);
        });
    }

    function handleFullscreenChange() {
        scheduleResize();

        // Exiting fullscreen with Esc may take several frames to finish resizing.
        window.setTimeout(scheduleResize, 50);
        window.setTimeout(scheduleResize, 150);
        window.setTimeout(scheduleResize, 300);
        window.setTimeout(scheduleResize, 500);
    }

    function initializeResizePatch() {
        if (!findUnityElements()) {
            window.setTimeout(initializeResizePatch, 100);
            return;
        }

        applyCanvasSize();

        window.addEventListener("resize", scheduleResize, {
            passive: true
        });

        window.addEventListener("orientationchange", scheduleResize, {
            passive: true
        });

        window.addEventListener("pageshow", scheduleResize);

        document.addEventListener(
            "fullscreenchange",
            handleFullscreenChange
        );

        document.addEventListener(
            "webkitfullscreenchange",
            handleFullscreenChange
        );

        document.addEventListener("visibilitychange", () => {
            if (!document.hidden) {
                scheduleResize();
            }
        });

        if (window.visualViewport) {
            window.visualViewport.addEventListener(
                "resize",
                scheduleResize,
                { passive: true }
            );

            window.visualViewport.addEventListener(
                "scroll",
                scheduleResize,
                { passive: true }
            );
        }

        if ("ResizeObserver" in window) {
            resizeObserver = new ResizeObserver(scheduleResize);
            resizeObserver.observe(unityContainer);
        }

        console.log("[WebGL Resize Patch] Initialized.");
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initializeResizePatch,
            { once: true }
        );
    } else {
        initializeResizePatch();
    }
})();