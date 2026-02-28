using UnityEngine;

public class LockChildRotation : MonoBehaviour
{
    /*
    Parent (Rotates)
        Pivot (Does not rotate) - Where this script has to be
            Child (Attached to Pivot)
    */
    void LateUpdate()
    {
        // Reset the child's rotation to identity (no rotation)
        transform.rotation = Quaternion.identity;
    }
}