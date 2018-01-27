using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntennaScript : MonoBehaviour {

    public Transform antenna;
    public bool canRotate = true;
    public float rotTime = .2f;
    public float minAngle = 0;
    public float maxAngle = 180;

    public Transform spawnBolt;
    public float minPosition = 0.75f;
    public float maxPosition = 4.25f;

    public bool isConnected = false;
    public Constants c;


    float counter = 0;
    Renderer myRend;

    private void Start()
    {
        myRend = antenna.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update () {
        if (!isConnected) return;
        counter += Time.deltaTime;
        if(counter >= rotTime)
        {
            counter -= rotTime;
            if(canRotate) antenna.Rotate(Vector3.forward * Random.Range(minAngle, maxAngle));
            Vector3 pos = spawnBolt.localPosition;
            pos.y = Random.Range(minPosition, maxPosition);
            spawnBolt.localPosition = pos;
        }
	}

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, c.antenaCollisionRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, c.antenaActivationRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, c.antenaLinkMaxRadius);
    }

    public void SetColor(Color c)
    {
        myRend.material.color = c;
    }
}
