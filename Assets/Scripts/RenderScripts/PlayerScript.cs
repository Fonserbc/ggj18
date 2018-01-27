using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour {

    public Renderer waveRenderer;
    public Renderer wheelRenderer;

    public float antenaActivationRadius = 1.5f;
    public float antenaCollisionRadius = 0.5f;

    public bool waving = false;
    public bool moving = false;
    public float speed = 5;

    float offset = 0;
    // Update is called once per frame
    void Update () {
        if (waving) waveRenderer.transform.Rotate(Vector3.up*5);
        if (moving) {
            offset -= (speed * Time.deltaTime)%1f;

            wheelRenderer.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
    }
}
