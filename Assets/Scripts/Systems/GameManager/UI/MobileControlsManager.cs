using UnityEngine;

public class MobileControlsManager : MonoBehaviour
{
    [Header("Mobile Controls")]
    [SerializeField] private MobileControls mobileControls;
    
    [Header("Settings")]
    [SerializeField] private float buttonPressedOpacity = 0.5f;
    [SerializeField] private float buttonUnpressedOpacity = 0.3f;
    
    private void Start()
    {
        UpdateControlsVisibility();
        LoadButtonSettings();
    }
    
    public void UpdateControlsVisibility()
    {
        if (mobileControls != null)
        {
            mobileControls.gameObject.SetActive(GlobalVariables.OnScreenControls);
            
            if (GlobalVariables.OnScreenControls)
            {
                mobileControls.UpdateButtonPosScaleOpacity();
            }
        }
    }
    
    public void UpdateButtonOpacity(float pressedOpacity, float unpressedOpacity)
    {
        buttonPressedOpacity = pressedOpacity;
        buttonUnpressedOpacity = unpressedOpacity;
        
        if (mobileControls != null)
        {
            mobileControls.UpdateButtonOpacity(pressedOpacity, unpressedOpacity);
        }
        
        SaveButtonSettings();
    }
    
    private void LoadButtonSettings()
    {
        // Cargar configuración guardada
        if (PlayerPrefs.HasKey("MobileBtnPressedOpacity"))
        {
            buttonPressedOpacity = PlayerPrefs.GetFloat("MobileBtnPressedOpacity", 0.5f);
            buttonUnpressedOpacity = PlayerPrefs.GetFloat("MobileBtnUnpressedOpacity", 0.3f);
            
            if (mobileControls != null)
            {
                mobileControls.UpdateButtonOpacity(buttonPressedOpacity, buttonUnpressedOpacity);
            }
        }
    }
    
    private void SaveButtonSettings()
    {
        PlayerPrefs.SetFloat("MobileBtnPressedOpacity", buttonPressedOpacity);
        PlayerPrefs.SetFloat("MobileBtnUnpressedOpacity", buttonUnpressedOpacity);
        PlayerPrefs.Save();
    }
    
    public void SetDefaultButtonOpacity()
    {
        UpdateButtonOpacity(0.5f, 0.3f);
    }
    
    public bool IsRunButtonPressed()
    {
        return GlobalVariables.mobileRunButtonPressed;
    }
}