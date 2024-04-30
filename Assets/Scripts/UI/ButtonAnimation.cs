
using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour
{
    Button btn;
    public Vector3 upScale = new(0.27f, 1.28f, 1.28f);
    public Vector3 normalScale = new(0.21f, 1f, 1f);

    // Start is called before the first frame update
    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(Anim);
    }

    void Anim()
    {
        LeanTween.scale(gameObject, upScale, 0.1f);
        LeanTween.scale(gameObject, normalScale, 0.1f).setDelay(0.1f);
    }
}
