using UnityEngine;
using System.Collections;

public class laserMove : MonoBehaviour {
	private bool _shoot;
	private Vector3 _originalPosition;
	public Transform _enemy;
	public Transform _player;
	// Use this for initialization
	void Start () {
		_originalPosition = this.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
	
		if (!_shoot) {
		
			this.transform.position = _originalPosition;
		
		}

		if (_shoot) {
		
			this.transform.position = Vector3.MoveTowards (this.transform.position, _player.position, 10 * Time.unscaledDeltaTime);
		
		}

		if(Input.GetKeyDown(KeyCode.Space)){

			Shoot ();
		}

	}

	public void Shoot(){
	
	
		_shoot = true;
	}

	void OnCollisionEnter(Collision p){
	
		_shoot = false;
	
	}

}
