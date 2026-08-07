using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// C# side of LumFilePicker.jslib.
///
/// Import: the browser dialog CANNOT be opened from a normal Unity Button
/// OnClick. <see cref="Arm"/> must be called from OnPointerDown (see
/// WebGLImportPointerTrigger), because the jslib defers input.click() to the
/// next real DOM mouseup, which has not happened yet at pointer-down time.
///
/// Export: <see cref="Download"/> can be called from anywhere.
///
/// A gamepad produces no mouseup, so import is mouse/touch only on WebGL. The
/// watchdog below resolves that case as a cancel instead of hanging.
/// </summary>
public sealed class WebGLFilePicker : MonoBehaviour
{
    /// Must match the GameObject name exactly — the jslib SendMessage()s to it.
    public const string PickerObjectName = "WebGLFilePicker";

    /// How long to wait for the browser to OPEN the dialog. This guards only the
    /// "no mouseup ever arrived" case (gamepad). Once the dialog is open the
    /// watchdog stands down, because the player may browse folders for minutes.
    private const float ArmTimeoutSeconds = 8f;

    /// Where the imported bytes are staged so the existing
    /// SaveManager.ImportSlot(int, string) path can stay unchanged.
    private const string StagingFileName = "__webgl_import";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void LumFilePicker_Open(
        string gameObjectName,
        string methodName,
        string filter,
        bool multiselect
    );

    [DllImport("__Internal")]
    private static extern void LumFilePicker_Cancel();

    [DllImport("__Internal")]
    private static extern void LumFilePicker_Download(
        string gameObjectName,
        string methodName,
        string fileName,
        byte[] bytes,
        int byteCount
    );

    [DllImport("__Internal")]
    private static extern void LumFilePicker_RevokeUrl(string url);
#endif

    private static WebGLFilePicker instance;

    private Action<string> pendingImportCallback;
    private string pendingExtensionWithDot = ".lummm";
    private Coroutine armWatchdog;
    private bool dialogArmed;
    private bool dialogOpened;
    private GameObject inputBlocker;

    /// <summary>True between Arm() and the callback firing.</summary>
    public bool IsBusy => dialogArmed;

    public static WebGLFilePicker Instance
    {
        get
        {
            if (instance != null)
                return instance;

            var existing = GameObject.Find(PickerObjectName);
            if (existing != null)
            {
                instance = existing.GetComponent<WebGLFilePicker>()
                           ?? existing.AddComponent<WebGLFilePicker>();
                return instance;
            }

            var go = new GameObject(PickerObjectName);
            DontDestroyOnLoad(go);
            instance = go.AddComponent<WebGLFilePicker>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // SendMessage targets this exact name. Anything that renames the object
        // silently breaks the callback.
        gameObject.name = PickerObjectName;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Arms the browser file dialog. MUST be called from OnPointerDown.
    /// <paramref name="onFileSelected"/> receives a real local path under
    /// Application.persistentDataPath, or null on cancel/failure. It always
    /// fires exactly once.
    /// </summary>
    /// <param name="extensionWithoutDot">e.g. "lummm". Pass "*" to disable filtering.</param>
    public void Arm(Action<string> onFileSelected, string extensionWithoutDot = "lummm")
    {
        if (onFileSelected == null)
            return;

        // Never let two requests overlap — resolve the old one as cancelled.
        if (dialogArmed)
        {
            Debug.LogWarning("WebGLFilePicker: re-arming while a request was pending.");
            ResolveImport(null);
        }

        string normalized = string.IsNullOrWhiteSpace(extensionWithoutDot)
            ? "*"
            : extensionWithoutDot.Trim().TrimStart('.');

        pendingExtensionWithDot = normalized == "*" ? string.Empty : "." + normalized;
        pendingImportCallback = onFileSelected;
        dialogArmed = true;
        dialogOpened = false;

        // The WebGL dialog does NOT block Unity, unlike SFB's SaveFilePanel on
        // desktop. Without this, the release of the very click that opened the
        // dialog reaches FileSelectManager.OnUIButtonClicked, which runs the
        // pipe sequence and PlayFocusedSlot alongside the import.
        SuppressInput();

#if UNITY_WEBGL && !UNITY_EDITOR
        string accept = normalized == "*" ? "*" : "." + normalized;
        LumFilePicker_Open(PickerObjectName, nameof(OnBrowserFileSelected), accept, false);

        armWatchdog = StartCoroutine(WatchForMissingGesture());
#else
        Debug.LogWarning("WebGLFilePicker.Arm called outside a WebGL player.");
        ResolveImport(null);
#endif
    }

    /// <summary>
    /// Cancels a pending request. Call this if the player backs out of import
    /// mode before clicking a slot.
    /// </summary>
    public void CancelPending()
    {
        if (!dialogArmed)
            return;

#if UNITY_WEBGL && !UNITY_EDITOR
        LumFilePicker_Cancel();
#endif
        ResolveImport(null);
    }

    /// <summary>
    /// Triggers a browser download. There is no Save As dialog and no path to
    /// choose — the browser decides where the file lands.
    /// </summary>
    public void Download(byte[] bytes, string fileNameWithExtension)
    {
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning("WebGLFilePicker.Download: nothing to write.");
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        LumFilePicker_Download(
            PickerObjectName,
            nameof(OnBrowserDownloadComplete),
            fileNameWithExtension,
            bytes,
            bytes.Length
        );
#else
        Debug.LogWarning("WebGLFilePicker.Download called outside a WebGL player.");
#endif
    }

    /// <summary>Raised after the browser has been handed the file.</summary>
    public event Action<string> DownloadStarted;

    // ---------------------------------------------------------------- browser

    /// Called from JS the moment input.click() fires. Stops the watchdog: from
    /// here on, waiting is expected and unbounded.
    public void OnBrowserDialogOpened()
    {
        dialogOpened = true;

        if (armWatchdog != null)
        {
            StopCoroutine(armWatchdog);
            armWatchdog = null;
        }
    }

    /// Called from JS. Newline-separated blob URLs, or "" for cancel.
    public void OnBrowserFileSelected(string payload)
    {
        if (armWatchdog != null)
        {
            StopCoroutine(armWatchdog);
            armWatchdog = null;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            Debug.Log("WebGLFilePicker: import cancelled.");
            ResolveImport(null);
            return;
        }

        string firstUrl = payload.Split('\n')[0].Trim();

        if (string.IsNullOrEmpty(firstUrl))
        {
            ResolveImport(null);
            return;
        }

        StartCoroutine(FetchAndStage(firstUrl));
    }

    /// Called from JS once the download has been handed off.
    public void OnBrowserDownloadComplete(string fileName)
    {
        Debug.Log($"WebGLFilePicker: browser download started for '{fileName}'.");
        DownloadStarted?.Invoke(fileName);
    }

    // ----------------------------------------------------------------- helpers

    /// <summary>
    /// Reads the blob into a real file under persistentDataPath so the rest of
    /// the save system keeps working with plain paths.
    /// </summary>
    private IEnumerator FetchAndStage(string blobUrl)
    {
        byte[] data = null;

        using (var request = UnityWebRequest.Get(blobUrl))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool failed = request.result != UnityWebRequest.Result.Success;
#else
            bool failed = request.isNetworkError || request.isHttpError;
#endif
            if (failed)
                Debug.LogError($"WebGLFilePicker: could not read blob: {request.error}");
            else
                data = request.downloadHandler.data;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        LumFilePicker_RevokeUrl(blobUrl);
#endif

        if (data == null || data.Length == 0)
        {
            ResolveImport(null);
            yield break;
        }

        string extension = string.IsNullOrEmpty(pendingExtensionWithDot)
            ? ".dat"
            : pendingExtensionWithDot;

        string stagedPath = Path.Combine(
            Application.persistentDataPath,
            StagingFileName + extension
        );

        try
        {
            File.WriteAllBytes(stagedPath, data);
        }
        catch (Exception e)
        {
            Debug.LogError($"WebGLFilePicker: could not stage import file: {e.Message}");
            ResolveImport(null);
            yield break;
        }

        ResolveImport(stagedPath);
    }

    /// <summary>
    /// If no mouseup ever arrives — a gamepad player, or the player navigated
    /// away — the jslib stays armed and the callback never fires, which would
    /// wedge SaveSlotManager's isFileDialogOpen flag for the session.
    /// </summary>
    private IEnumerator WatchForMissingGesture()
    {
        float deadline = Time.unscaledTime + ArmTimeoutSeconds;

        while (dialogArmed && !dialogOpened && Time.unscaledTime < deadline)
            yield return null;

        armWatchdog = null;

        if (!dialogArmed || dialogOpened)
            yield break;

        Debug.LogWarning(
            "WebGLFilePicker: no mouse gesture reached the browser within " +
            $"{ArmTimeoutSeconds}s — treating as cancelled. On WebGL the import " +
            "dialog cannot be opened with a gamepad."
        );

#if UNITY_WEBGL && !UNITY_EDITOR
        LumFilePicker_Cancel();
#endif
        ResolveImport(null);
    }

    /// <summary>
    /// Raises a transparent full-screen raycast blocker so clicks cannot reach
    /// the game while a non-blocking browser dialog is open on top of it.
    ///
    /// This deliberately does NOT disable the EventSystem. Disabling it does not
    /// cancel the in-flight press — the input module keeps the press state and
    /// replays the click on re-enable, which lands right when the import
    /// callback is running. A blocker keeps the EventSystem live, so the button
    /// still receives its pointer-up (no stuck pressed state), but the pointer
    /// is now over the blocker rather than the button, so uGUI never promotes
    /// the release into a click.
    /// </summary>
    private void SuppressInput()
    {
        if (inputBlocker != null)
        {
            inputBlocker.SetActive(true);
            return;
        }

        inputBlocker = new GameObject("WebGLFilePickerInputBlocker");
        inputBlocker.transform.SetParent(transform, false);

        var canvas = inputBlocker.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        inputBlocker.AddComponent<GraphicRaycaster>();

        var image = inputBlocker.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void RestoreInput()
    {
        if (inputBlocker != null)
            inputBlocker.SetActive(false);
    }

    /// <summary>Fires the pending callback exactly once and clears state.</summary>
    private void ResolveImport(string pathOrNull)
    {
        Action<string> callback = pendingImportCallback;

        pendingImportCallback = null;
        dialogArmed = false;
        dialogOpened = false;

        // Restore BEFORE the callback: ProcessImportFileSelection calls
        // EventSystem.SetSelectedGameObject and needs a live EventSystem.
        RestoreInput();

        if (armWatchdog != null)
        {
            StopCoroutine(armWatchdog);
            armWatchdog = null;
        }

        callback?.Invoke(pathOrNull);
    }
}