using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FileSelectMarioController : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float snapDistance = 0.02f;

    public Animator animator;
    public bool IsBusy { get; private set; }

    Transform marioTransform;
    Transform target;

    void Awake()
    {
        marioTransform = transform;
    }

    void Update()
    {
        // Update target based on current selected UI
        var current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            var provider = current.GetComponent<MarioAnchorProvider>();
            if (provider != null && provider.marioAnchor != null)
                target = provider.marioAnchor;
        }

        // Move towards target if not doing a special sequence
        if (!IsBusy && target != null)
        {
            marioTransform.position = Vector3.Lerp(
                marioTransform.position,
                target.position,
                Time.deltaTime * moveSpeed
            );
        }
    }

    public IEnumerator MoveTo(Transform destination)
    {
        IsBusy = true;

        while (Vector3.Distance(marioTransform.position, destination.position) > snapDistance)
        {
            marioTransform.position = Vector3.Lerp(
                marioTransform.position,
                destination.position,
                Time.deltaTime * moveSpeed
            );
            yield return null;
        }

        marioTransform.position = destination.position;
        IsBusy = false;
    }

    public void SetIdle()
    {
        animator.SetTrigger("Idle");
    }

    public void SetJump()
    {
        animator.SetTrigger("Jump");
    }

    public void SetBomb()
    {
        animator.SetTrigger("Bomb");
    }
}