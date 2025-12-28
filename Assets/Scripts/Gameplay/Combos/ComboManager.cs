using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Combo Sets")]
    public ComboSet stompCombo;
    public ComboSet shellCombo;
    public ComboSet starCombo;

    [Header("Audio")]
    public AudioClip oneUpSound;
    private AudioSource audioSource;

    private int stompIndex = -1;
    private int shellIndex = -1;
    private int starIndex = -1;

    public bool ShellChainActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public ComboResult RegisterStompKill()
    {
        return GetNext(stompCombo, ref stompIndex);
    }

    public ComboResult RegisterShellKill()
    {
        return GetNext(shellCombo, ref shellIndex);
    }

    public ComboResult RegisterStarKill()
    {
        return GetNext(starCombo, ref starIndex);
    }

    public void StartShellChain()
    {
        ShellChainActive = true;
        shellIndex = -1;
    }

    public void EndShellChain()
    {
        ShellChainActive = false;
        shellIndex = -1;
    }

    public void ResetStomp()
    {
        stompIndex = -1;
    }

    public void ResetShellChain()
    {
        shellIndex = -1;
    }

    public void ResetAll()
    {
        stompIndex = -1;
        shellIndex = -1;
        starIndex = -1;
        ShellChainActive = false;
    }

    private ComboResult GetNext(ComboSet set, ref int index)
    {
        if (set == null || set.steps == null || set.steps.Length == 0)
            return new ComboResult(RewardType.Score, PopupID.Score100, 0);

        index++;

        if (index >= set.steps.Length)
            index = set.steps.Length - 1;

        ComboStep step = set.steps[index];

        if (step.rewardType == RewardType.OneUp)
        {
            PlayOneUpSound();
        }

        return new ComboResult(step.rewardType, step.popupID, step.amount);
    }

    public void PlayOneUpSound()
    {
        if (audioSource != null && oneUpSound != null)
        {
            audioSource.PlayOneShot(oneUpSound);
        }
    }
}