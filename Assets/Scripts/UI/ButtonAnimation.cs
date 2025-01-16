
using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour
{
    Button btn;
    public float upScaleAmount = 1.5f;
    private Vector3 defaultScale;

    // Start is called before the first frame update
    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(Anim);
        defaultScale = transform.localScale;
    }

    void Anim()
    {
        LeanTween.scale(gameObject, defaultScale * upScaleAmount, 0.1f);
        LeanTween.scale(gameObject, defaultScale, 0.1f).setDelay(0.1f);
    }
}
