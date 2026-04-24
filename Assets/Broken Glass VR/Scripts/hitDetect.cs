using UnityEngine;
using System.Collections;

public class hitDetect : MonoBehaviour {


	void Start () {
	
	}
	

	void Update () {
	
	}

	void OnCollisionEnter(Collision c){
	
		GetComponentInParent<brokenGlassVR> ().GlassShatter ();
	
	}
}
