using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System.Collections;
using System;

public class TimedActionWithButtonSkip : MonoBehaviour
{ 
    [Header("Timing Settings")]
    public float delayBeforeAction = 5f;
    public bool startTimerOnEnable = true;
    
    [Header("Input Settings")]
    public bool useAnyButtonPress = true;
    public bool allowMultipleTriggers = false;
    
    [Header("Events")]
    public UnityEvent OnActionTriggered; // Fires when action is triggered (button or timeout)
    public UnityEvent OnButtonPressed; // Fires specifically on button press
    public UnityEvent OnTimeout; // Fires specifically on timeout
    public UnityEvent OnTimerStarted; // Fires when timer begins

    private bool actionTriggered = false;
    private IDisposable m_EventListener;
    private Coroutine timerCoroutine;

    private void OnEnable()
    {
        if (useAnyButtonPress)
        {
            m_EventListener = InputSystem.onAnyButtonPress
                .Call(ctrl =>
                {
                    if (!actionTriggered || allowMultipleTriggers)
                    {
                        OnButtonPressed?.Invoke();
                        TriggerAction();
                    }
                });
        }

        if (startTimerOnEnable)
        {
            StartTimer();
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from global button presses
        m_EventListener?.Dispose();
        m_EventListener = null;

        // Stop timer if running
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    // Public method to start timer manually
    public void StartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        actionTriggered = false;
        timerCoroutine = StartCoroutine(TimerCoroutine());
        OnTimerStarted?.Invoke();
    }

    // Public method to trigger action manually
    public void TriggerAction()
    {
        if (actionTriggered && !allowMultipleTriggers) return;
        
        actionTriggered = true;
        OnActionTriggered?.Invoke();
        
        // Stop the timer if it's running
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    // Public method to trigger timeout manually
    public void TriggerTimeout()
    {
        if (actionTriggered && !allowMultipleTriggers) return;
        
        actionTriggered = true;
        OnTimeout?.Invoke();
        OnActionTriggered?.Invoke();
    }

    private IEnumerator TimerCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeAction);

        if (!actionTriggered || allowMultipleTriggers) 
        {
            TriggerTimeout();
        }
    }

    // Reset the component to allow retriggering
    public void ResetTrigger()
    {
        actionTriggered = false;
    }
}