using UnityEngine;
using System.Collections;

public class PongBallScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 v3Velocity = rb.velocity;

	}
}
