using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageScript : MonoBehaviour {

    public Renderer messageRend;

    public AudioClip[] popUp;
    public AudioClip[] travel;
    public AudioClip[] send;
    public AudioSource myAudio;

    public int currentAntena = -1;

    bool isSending = false;

    void Start()
    {
        myAudio.clip = popUp[Random.Range(0, popUp.Length)];
        myAudio.Play();        
    }

    public void playTravelAudio()
    {
        myAudio.Stop();
        myAudio.clip = travel[Random.Range(0, travel.Length)];
        myAudio.Play();
    }

    public void playSendAudio()
    {
        if (isSending) return;
        isSending = true;
        myAudio.Stop();
        myAudio.clip = send[Random.Range(0, send.Length)];
        myAudio.Play();
    }


}
