using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TinyLivesDownEvent : MonoBehaviour
{
    private PlayableDirector TimelineDirector;

    public TimelineAsset livesGoDownTimeline;
    public TimelineAsset nervousLivesGoDownTimeline;

    // Start is called before the first frame update
    void Start()
    {
        TimelineDirector = GetComponent<PlayableDirector>();

        if (GlobalVariables.lives == 1 && nervousLivesGoDownTimeline != null)
        {
            TimelineDirector.Play(nervousLivesGoDownTimeline);
        }
        else if (livesGoDownTimeline != null)
        {
            TimelineDirector.Play(livesGoDownTimeline);
        }
    }
}
