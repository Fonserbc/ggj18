using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntennaScript : MonoBehaviour {

    public Transform antenna;
    public Renderer antennaRend;
    public Transform messageHolder;
    public bool canRotate = true;
    public bool randomRotation = true;
    public float rotTime = .2f;
    public float minAngle = 0;
    public float maxAngle = 180;
    public Vector3 rotationDir = Vector3.forward;
    public float rotationSpeed = 90f;

    public Transform spawnBolt;
    public float minPosition = 0.75f;
    public float maxPosition = 4.25f;

    public Transform[] messageHolders;

    public bool isConnected = false;
    public Constants c;


    float counter = 0;



    // Update is called once per frame
    void Update () {

         messageHolder.Rotate(Vector3.up * rotationSpeed*.5f * Time.deltaTime);
        

        if (!isConnected) return;

        if(canRotate && !randomRotation) antenna.Rotate(rotationDir * rotationSpeed * Time.deltaTime);

        counter += Time.deltaTime;
        if(counter >= rotTime)
        {
            counter -= rotTime;
            if (canRotate)
            {
                if(randomRotation) antenna.Rotate(rotationDir * Random.Range(minAngle, maxAngle));
            }
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
        antennaRend.material.color = c;
    }
}
