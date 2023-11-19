using UnityEngine;
using System.Collections;
    
    public class MusicDontDestroy : MonoBehaviour {
    
        private GameObject[] music;
    
        void Start(){
            // NOTE: As far as I can tell, the new music calls this first before the old music calls it.
            // So it should make the object destroy itself if there is already another music object.
            music = GameObject.FindGameObjectsWithTag (this.tag);
            if (music.Length > 1) {
                // check if the music is muted (overridden already)
                if (music[1].GetComponent<AudioSource>().mute) {
                    // mute currently playing music
                    print("muting");
                    GetComponent<AudioSource>().mute = true;
                }
                
                // set currently playing music to new music
                if (GameManager.Instance != null) {
                    GameManager.Instance.SetNewMainMusic(music[0]);
                    print("set new music");
                }

                // destroy new music (this one)
                print("destroying");
                Destroy (music[1]);
                
            }
        }
        
        // Update is called once per frame
        void Awake () {
            DontDestroyOnLoad (transform.gameObject);
        }
    }