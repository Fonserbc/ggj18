using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicSoundScript : MonoBehaviour {
    public AudioClip[] pressSounds;
    public AudioSource pressAudio;
	// Use this for initialization
	void Start () {
        pressAudio.clip = pressSounds[Random.Range(0, pressSounds.Length)];
        pressAudio.Play();
        Destroy(gameObject, .5f);
	}
	
}
