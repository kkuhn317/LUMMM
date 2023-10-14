using UnityEngine;
using System.Collections;
    
    public class MusicDontDestroy : MonoBehaviour {
    
        private GameObject[] music;
    
        void Start(){
            music = GameObject.FindGameObjectsWithTag (this.tag);
            if (music.Length > 1) {
                Destroy (music[1]);
                // set me as new music
                if (GameManager.Instance != null)
                    GameManager.Instance.music = music[0];
            }
            
        }
        
        // Update is called once per frame
        void Awake () {
            DontDestroyOnLoad (transform.gameObject);
        }
    }