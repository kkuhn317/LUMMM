using System;
using UnityEngine;

public class DoNotDestroy : MonoBehaviour
{

    public enum DuplicateAction
    {
        None,
        DestroyExisting,
        DestroyMyself
    }

    [SerializeField] DuplicateAction duplicateAction = DuplicateAction.DestroyMyself;


    private static DoNotDestroy instance;

    private void Awake()
    {
        switch (duplicateAction)
        {
            case DuplicateAction.None:
                break;
            case DuplicateAction.DestroyExisting:
                if (instance == null) {
                    instance = this;
                    DontDestroyOnLoad(gameObject);
                } else {
                    Destroy(instance.gameObject);
                    instance = this;
                    DontDestroyOnLoad(gameObject);
                }
                break;
                
            case DuplicateAction.DestroyMyself:
                if (instance == null) {
                    instance = this;
                    DontDestroyOnLoad(gameObject);
                } else {
                    Destroy(gameObject);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }
}
