using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSwitch : MonoBehaviour
{
    public List<GameObject> bricks = new List<GameObject>();
    public List<GameObject> coins = new List<GameObject>();
    private List<GameObject> convertedBricks = new List<GameObject>();
    private List<GameObject> convertedCoins = new List<GameObject>();
    public GameObject brickPrefab;
    public GameObject coinPrefab;
    public float effectDuration = 10f; // Duration of the effect in seconds

    public AudioClip switchSound; // Sound to play when the switch is activated
    public AudioSource soundSource;
    public AudioSource musicSource;

    private void Start() {

    }
    
    private void OnCollisionEnter2D(Collision2D other) {
        Vector2 impulse = Vector2.zero;

        int contactCount = other.contactCount;
        for(int i = 0; i < contactCount; i++) {
            var contact = other.GetContact(i);
            impulse += contact.normal * contact.normalImpulse;
            #if !UNITY_ANDROID
            impulse.x += contact.tangentImpulse * contact.normal.y;
            impulse.y -= contact.tangentImpulse * contact.normal.x;
            #endif
            // Using the same logic as the trampoline, but we don't need to worry about the player activating the switch from the side
        }

        if (impulse.y < 0) {
            if (other.gameObject.tag == "Player") {
                ActivateSwitch();
            }
        }
    }

    private void ActivateSwitch() {
        // Play the animation
        // Animator animator = GetComponent<Animator>();
        // if (animator != null)
        //     animator.SetTrigger("Activate");

        // For now, just make the switch disappear visually, and remove the collider
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        // Play the sound
        if (switchSound != null && soundSource != null) {
            soundSource.Play();
        }

        // Play the music
        if (musicSource != null)
            musicSource.Play();

        // Override the music
        GameManager.Instance.OverrideMusic(this.gameObject);

        // Change the bricks to coins and add to converted lists
        for (int i = 0; i < bricks.Count; i++) {
            GameObject brick = bricks[i];
            if (brick != null) {
                GameObject coin = Instantiate(coinPrefab, brick.transform.position, Quaternion.identity);
                Destroy(brick);
                convertedCoins.Add(coin);
            }
        }
        bricks.Clear();

        // Change the coins to bricks and add to converted lists
        for (int i = 0; i < coins.Count; i++) {
            GameObject coin = coins[i];
            if (coin != null) {
                GameObject brick = Instantiate(brickPrefab, coin.transform.position, Quaternion.identity);
                Destroy(coin);
                convertedBricks.Add(brick);
            }
        }
        coins.Clear();

        // Start the coroutine to revert the effect after a delay
        StartCoroutine(RevertEffectAfterDelay(effectDuration));
    }

    private IEnumerator RevertEffectAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);

        // Change the bricks back to coins
        for (int i = 0; i < convertedBricks.Count; i++) {
            GameObject brick = convertedBricks[i];
            if (brick != null) {
                GameObject coin = Instantiate(coinPrefab, brick.transform.position, Quaternion.identity);
                Destroy(brick);
                coins.Add(coin);
            }
        }
        convertedBricks.Clear();

        // Change the coins back to bricks
        for (int i = 0; i < convertedCoins.Count; i++) {
            GameObject coin = convertedCoins[i];
            if (coin != null) {
                GameObject brick = Instantiate(brickPrefab, coin.transform.position, Quaternion.identity);
                Destroy(coin);
                bricks.Add(brick);
            }
        }
        convertedCoins.Clear();

        GameManager.Instance.ResumeMusic(this.gameObject);
        Destroy(gameObject);
    }
}

