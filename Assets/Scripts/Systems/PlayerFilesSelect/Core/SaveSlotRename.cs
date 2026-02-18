using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.EventSystems;
using System.Collections;

public class SaveSlotRename : MonoBehaviour
{
    public static SaveSlotRename ActivePopup { get; private set; }
    public static bool IsAnyPopupOpen => ActivePopup != null && ActivePopup.IsOpen;

    public bool IsOpen => renameUI != null && renameUI.activeSelf;
    public bool IsCreatingNew => isCreatingNew;

    [Header("UI References")]
    public GameObject renameUI;
    public TMP_InputField nameInputField;
    public Button confirmButton;
    public Button cancelButton;
    public TMP_Text placeholderText;
    public TMP_Text titleText;

    [Header("Settings")]
    public int maxNameLength = 15;
    public LocalizedString renameTitle;
    public LocalizedString createTitle;
    public LocalizedString placeholderTextKey;

    [Header("Events")]
    public UnityEvent onRenameOpened;
    public UnityEvent<int, string> onNameConfirmed;
    public UnityEvent onRenameCancelled;

    [Header("Cancel Input")]
    [Tooltip("When popup is open, this action will trigger the No button")]
    public InputActionReference cancelAction;

    private int targetSlotIndex = -1;
    private bool isCreatingNew = false;

    // this is used to prevent selection flicker or double-submit weirdness
    private bool isClosing;
    private Coroutine pendingSelectRoutine;
    private GameObject returnSelectedOnClose;

    private void OnEnable()
    {
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed += OnCancelPerformed;
            if (!cancelAction.action.enabled)
                cancelAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
            if (cancelAction.action.enabled)
                cancelAction.action.Disable();
        }
    }

    private void Start()
    {
        if (renameUI != null)
            renameUI.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmRename);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelRename);

        if (nameInputField != null)
        {
            nameInputField.characterLimit = maxNameLength;
            nameInputField.onValueChanged.AddListener(OnInputChanged);

            // Enter/Submit should NOT confirm. It should move focus to a button.
            nameInputField.onSubmit.AddListener(OnInputSubmit);
        }

        if (titleText == null && renameUI != null)
            titleText = renameUI.GetComponentInChildren<TMP_Text>(true);

        if (placeholderText != null && placeholderTextKey != null)
            placeholderText.text = placeholderTextKey.GetLocalizedString();
    }

    public void OpenForRename(int slotIndex, string currentName = "")
    {
        isClosing = false;

        // Remember what was selected BEFORE opening (so we can restore later)
        returnSelectedOnClose = EventSystem.current?.currentSelectedGameObject;

        onRenameOpened?.Invoke();

        targetSlotIndex = slotIndex;
        isCreatingNew = false;

        if (nameInputField != null)
        {
            nameInputField.text = currentName;
            StartCoroutine(FocusNextFrame());
        }

        UpdateTitle();

        if (renameUI != null)
        {
            renameUI.SetActive(true);
            ActivePopup = this;
            // Ensure popup renders above Mario UI even if Mario was re-ordered during detach
            gameObject.transform.SetAsLastSibling();
        }

        OnInputChanged(nameInputField != null ? nameInputField.text : "");
    }

    public void OpenForNewSlot(int slotIndex)
    {
        isClosing = false;

        // Remember what was selected BEFORE opening
        returnSelectedOnClose = EventSystem.current?.currentSelectedGameObject;

        onRenameOpened?.Invoke();

        targetSlotIndex = slotIndex;
        isCreatingNew = true;

        if (nameInputField != null)
        {
            nameInputField.text = "";
            StartCoroutine(FocusNextFrame());
        }

        UpdateTitle();

        if (renameUI != null)
        {
            renameUI.SetActive(true);
            ActivePopup = this;
            // Ensure popup renders above Mario UI even if Mario was re-ordered during detach
            gameObject.transform.SetAsLastSibling();
        }

        OnInputChanged(nameInputField != null ? nameInputField.text : "");
    }

    private IEnumerator FocusNextFrame()
    {
        yield return null;
        if (nameInputField == null) yield break;

        nameInputField.Select();
        nameInputField.ActivateInputField();
    }

    private void UpdateTitle()
    {
        if (titleText == null) return;

        titleText.text = isCreatingNew
            ? (createTitle != null ? createTitle.GetLocalizedString() : "NAME SLOT")
            : (renameTitle != null ? renameTitle.GetLocalizedString() : "RENAME SLOT");
    }

    private void OnInputChanged(string text)
    {
        if (confirmButton != null)
            confirmButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    private void OnInputSubmit(string _)
    {
        // While closing, ignore Submit so it doesn't fight with restore logic.
        if (!IsOpen || isClosing) return;

        GameObject target =
            (confirmButton != null && confirmButton.interactable) ? confirmButton.gameObject :
            (cancelButton != null ? cancelButton.gameObject : null);

        if (target == null) return;

        // Cancel any pending selection move (avoid stacking coroutines)
        if (pendingSelectRoutine != null)
            StopCoroutine(pendingSelectRoutine);

        pendingSelectRoutine = StartCoroutine(SelectNextFrame(target));
    }

    private IEnumerator SelectNextFrame(GameObject go)
    {
        yield return null;
        pendingSelectRoutine = null;
        EventSystem.current?.SetSelectedGameObject(go);
    }

    public void ConfirmRename()
    {
        if (targetSlotIndex < 0 || nameInputField == null)
            return;

        string newName = nameInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(newName))
            newName = SaveSlotNaming.DefaultNameFor((SaveSlotId)targetSlotIndex);

        // Close FIRST, then invoke event AFTER selection restore (prevents ButtonSelector flicker)
        StartCoroutine(CloseAndConfirmNextFrames(targetSlotIndex, newName));
    }

    private IEnumerator CloseAndConfirmNextFrames(int slotIndex, string name)
    {
        isClosing = true;

        // Stop any pending "select confirm/cancel next frame"
        if (pendingSelectRoutine != null)
        {
            StopCoroutine(pendingSelectRoutine);
            pendingSelectRoutine = null;
        }

        // Clear selection immediately so selector/UI doesn't snap to confirm/cancel for 1 frame
        EventSystem.current?.SetSelectedGameObject(null);

        if (renameUI != null)
            renameUI.SetActive(false);

        if (ActivePopup == this)
            ActivePopup = null;

        // Let UI disable propagate
        yield return null;

        // Restore previous selection first (so when external listeners enable ButtonSelector it uses the right target)
        if (returnSelectedOnClose != null)
            EventSystem.current?.SetSelectedGameObject(returnSelectedOnClose);

        // Give EventSystem one more frame to settle
        yield return null;

        onNameConfirmed?.Invoke(slotIndex, name);

        isClosing = false;
    }

    public void CancelRename()
    {
        StartCoroutine(CloseAndCancelNextFrames());
    }

    private IEnumerator CloseAndCancelNextFrames()
    {
        isClosing = true;

        if (pendingSelectRoutine != null)
        {
            StopCoroutine(pendingSelectRoutine);
            pendingSelectRoutine = null;
        }

        EventSystem.current?.SetSelectedGameObject(null);

        if (renameUI != null)
            renameUI.SetActive(false);

        if (ActivePopup == this)
            ActivePopup = null;

        yield return null;

        if (returnSelectedOnClose != null)
            EventSystem.current?.SetSelectedGameObject(returnSelectedOnClose);

        yield return null;

        onRenameCancelled?.Invoke();

        isClosing = false;
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        // Only respond if this popup is active and cancel button exists
        if (gameObject.activeInHierarchy && cancelButton != null && cancelButton.interactable)
        {
            Debug.Log($"ConfirmPopup: Cancel action triggered on {gameObject.name}, clicking No");
            EventSystem.current?.SetSelectedGameObject(cancelButton.gameObject);
            cancelButton.onClick.Invoke();
        }
    }
}