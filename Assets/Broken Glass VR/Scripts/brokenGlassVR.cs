using UnityEngine;
using System.Collections;

public class brokenGlassVR : MonoBehaviour {
	private Material _glassMat;
	private bool _cracked;
	private float _distortion;
	public float _repairSpeed;

	void Start () {
		_glassMat = GetComponent<MeshRenderer> ().material;
	}
	

	void Update () {
		_glassMat.SetFloat ("_BumpAmt", _distortion);
		if (_cracked) {
		
			_distortion = Mathf.MoveTowards (_distortion, 0f, _repairSpeed * Time.unscaledDeltaTime);
			if (_distortion == 0f) {
			
				_cracked = false;
			
			}
		}

	}

	public void GlassShatter(){
	
		_distortion = 128f;
		_cracked = true;
	
	}

}
