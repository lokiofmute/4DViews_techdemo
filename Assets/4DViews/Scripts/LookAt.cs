using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using unity4dv;

public class LookAt : MonoBehaviour
{
    private Plugin4DS plugin;

    public Transform target;

    public int maxAngle=70;

    // Start is called before the first frame update
    void Start()
    {
        plugin = GetComponent<Plugin4DS>();
    }

    // Update is called once per frame
    void Update()
    {
        if (plugin && target) {
            Vector3 relativePos = target.position;
            relativePos = plugin.transform.InverseTransformPoint(relativePos);

            plugin.LookAtTarget = relativePos;
            plugin.LookAtMaxAngle = maxAngle;
        }
    }
}
