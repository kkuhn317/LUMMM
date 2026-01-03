// ComboManager.cs
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
    private int starIndex  = -1;

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
        if (!ShellChainActive)
            StartShellChain();

        return GetNext(shellCombo, ref shellIndex);
    }

    public ComboResult RegisterStarKill()
    {
        return GetNext(starCombo, ref starIndex);
    }

    /// <summary>
    /// Starts a fresh shell chain. Used when a shell begins moving.
    /// </summary>
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

    /// <summary>
    /// Read the last stomp amount WITHOUT advancing the stomp combo.
    /// Used for kick scaling (400 -> 500 -> 800) based on prior stomp rewards.
    /// </summary>
    public int PeekLastStompAmount()
    {
        if (stompCombo == null || stompCombo.steps == null || stompCombo.steps.Length == 0)
            return 0;

        if (stompIndex < 0)
            return 0;

        int idx = Mathf.Clamp(stompIndex, 0, stompCombo.steps.Length - 1);
        return stompCombo.steps[idx].amount;
    }

    /// <summary>
    /// Registers the shell KICK reward (the act of kicking the shell),
    /// without touching stomp combo and without spawning any extra rewards.
    /// 
    /// IMPORTANT: We assume StartShellChain() already ran (e.g., in ToMovingShell).
    /// We set shellIndex = 0 so the first shell KILL after the kick becomes step 1.
    /// </summary>
    public ComboResult RegisterShellKick(int kickPoints, PopupID kickPopupId)
    {
        ShellChainActive = true;

        // Kick counts as "step 0" for shell combo progression,
        // so the next RegisterShellKill() yields step 1.
        shellIndex = 0;

        return new ComboResult(RewardType.Score, kickPopupId, kickPoints);
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