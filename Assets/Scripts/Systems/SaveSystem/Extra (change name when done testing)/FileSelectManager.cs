using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class FileSelectManager : MonoBehaviour
{
    [Header("References")]
    public FileSelectMarioController mario;
    // public CameraShaker cameraShaker;
    public SaveSlotManager slotManager;

    public Transform cancelAnchor;
    public UnityEvent onCancel;

    private bool inputLocked;

    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed || inputLocked)
            return;

        HandleSubmit();
    }
        public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed || inputLocked)
            return;

        StartCoroutine(HandleCancel());
    }

    private void HandleSubmit()
    {
        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null)
            return;

        var interactable   = current.GetComponent<FileSelectInteractable>();
        var anchorProvider = current.GetComponent<MarioAnchorProvider>();

        if (interactable == null || anchorProvider == null || anchorProvider.marioAnchor == null)
            return;

        StartCoroutine(HandleAction(interactable, anchorProvider.marioAnchor));
    }

    private IEnumerator HandleAction(FileSelectInteractable interactable, Transform anchor)
    {
        inputLocked = true;

        // 1) Mover a Mario a la posición del objeto seleccionado
        if (mario != null && anchor != null)
        {
            yield return StartCoroutine(mario.MoveTo(anchor));
        }

        // 2) Ejecutar la secuencia según el tipo de acción
        switch (interactable.actionType)
        {
            case FileSelectActionType.DeleteSlot:
                yield return StartCoroutine(DeleteSlotSequence(interactable.slotIndex));
                break;

            case FileSelectActionType.EnterSlot:
                yield return StartCoroutine(EnterSlotSequence(interactable.slotIndex));
                break;

            // Más adelante puedes añadir:
            // case FileSelectActionType.CopySlot:
            //     yield return StartCoroutine(CopySlotSequence(interactable.slotIndex));
            //     break;
        }

        inputLocked = false;
    }

    private IEnumerator DeleteSlotSequence(int slotIndex)
    {
        // Mario → bomba (animación de borrar)
        if (mario != null)
            mario.SetBomb();

        // Darle un poco de tiempo a la animación para arrancar
        yield return new WaitForSeconds(0.3f);

        // Shake de cámara / impacto
        /*if (cameraShaker != null)
            cameraShaker.Shake(0.3f, 0.15f);*/

        // Borrar el slot y refrescar UI vía SaveSlotManager
        if (slotManager != null)
            slotManager.DeleteSlot(slotIndex);   // helper en SaveSlotManager

        // Pausa pequeña después de la explosión
        yield return new WaitForSeconds(0.2f);

        if (mario != null)
            mario.SetIdle();
    }

    private IEnumerator EnterSlotSequence(int slotIndex)
    {
        // Animación de “entrar por la tubería”
        if (mario != null)
            mario.SetJump();   // o SetEnterPipe() si haces un trigger específico

        yield return new WaitForSeconds(0.4f);

        // Cargar / jugar ese slot vía SaveSlotManager
        if (slotManager != null)
            slotManager.PlaySlot(slotIndex);     // helper en SaveSlotManager
    }

    private IEnumerator HandleCancel()
    {
        inputLocked = true;

        // Opcional: mover a Mario a un anchor de "salida"
        if (mario != null && cancelAnchor != null)
        {
            yield return StartCoroutine(mario.MoveTo(cancelAnchor));
            mario.SetIdle();
        }

        // Ejecutar lógica de volver atrás / cerrar menú
        onCancel?.Invoke();

        inputLocked = false;
    }
}