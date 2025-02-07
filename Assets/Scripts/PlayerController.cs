﻿using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	
	public float waitToIdle = 5; //TODO: Time to wait to set idle anim to player.
	public float velocity = 5;
	public float turnSpeed = 10;
    public LevelLoader levelLoader;

	private int _torchesToLightToOpenDoor;
	private int _torchesLighted = 0;
	private bool _torchTriggerEntered = false;
    private bool _isLightingAction = false;

    private TorchController _torchController;
	private CameraController _cameraController;
    private HintController _hintController;
	private Vector2 _input;
	private float _angle;
	private Quaternion _targetRotation;
	private Transform _cam;
	private Animator _animator;
	private Animator _doorAnimator;
	private AudioSource _audioFireIgnite;
	private AudioSource _audioOpenDoor;
    private AnalyticsController _analyticsController;
	
	private static readonly int LightTorchAnimation = Animator.StringToHash("LightTorch");
	private static readonly int Open = Animator.StringToHash("Open");
	private static readonly int IsWalking = Animator.StringToHash("IsWalking");

	//IMPORTANT INFO!
	//The player has a global capsule collider.
	//And also has a specific collider in the right hand middle finger to avoid "trespassing walls".
	
	public int GetTorchedLighted()
	{
		return _torchesLighted;
	}

	private void Start ()
	{
		if (Camera.main != null) _cam = Camera.main.transform;
		_animator = GetComponent<Animator> ();
		_audioFireIgnite = GetComponent<AudioSource>();
		_cameraController = GameObject.Find("Main Camera").GetComponent<CameraController>();
        if (GameObject.Find("HintCanvas") != null)
        {
            _hintController = GameObject.Find("HintCanvas").GetComponent<HintController>();
        }
		_doorAnimator = GameObject.Find ("Door.001").GetComponent<Animator> ();
		_audioOpenDoor= GameObject.Find ("Door.001").GetComponent<AudioSource> ();
        _torchesToLightToOpenDoor = GameObject.FindGameObjectsWithTag("LightStickOpenDoor").Length;
        _analyticsController = GetComponent<AnalyticsController>();
        _analyticsController.setLevel(levelLoader.GetCurrentLevel());
    }

	private void Loadlevel()
	{
        //Register time to complete the level.
        _analyticsController.LevelComplete();
        levelLoader.FadeToNextLevel();
	}

	private void Update ()
	{
        //Don't do nothing while the player is lighting a torch
         if (_animator.GetCurrentAnimatorStateInfo (0).IsTag ("Lighting") || _isLightingAction)
            return;

        //Check if the player pressed 'space' in front of a torch and light it if can.
        LightTorch ();

        ///Get input keys (move keys) and check if the player is moving or not.
		GetInput ();
		if (Mathf.Abs (_input.x) < 1 && Mathf.Abs (_input.y) < 1) {
			_animator.SetBool (IsWalking, false);
			return;
		}
        _animator.SetBool(IsWalking, true);

        //Camera and movement actions.
        CalculateDirection ();
		Rotate ();
		Move ();
	}

	/// <summary>
	/// Input based on Horizontal (a,d,<,>) and Vertival (w,s,^,v) keys
	/// </summary>
	private void GetInput ()
	{
		_input.x = Input.GetAxisRaw ("Horizontal");
		_input.y = Input.GetAxisRaw ("Vertical");
	}


    /// <summary>
    /// Direction relative to the camera's rotation
    /// </summary>
    private void CalculateDirection ()
	{
		_angle = Mathf.Atan2 (_input.x, _input.y);
		_angle = Mathf.Rad2Deg * _angle;
		_angle += _cam.eulerAngles.y;
	}

	/// <summary>
	/// Rotate toward the calculated angle
	/// </summary>
	private void Rotate ()
	{
		_targetRotation = Quaternion.Euler (0, _angle, 0);
		transform.rotation = Quaternion.Slerp (transform.rotation, _targetRotation, turnSpeed * Time.deltaTime);
	}

	/// <summary>
	/// This player only moves along its own forward axis.
	/// </summary>
	private void Move ()
	{
		transform.position += transform.forward * velocity * Time.deltaTime;
	}

    private void OnTriggerExit(Collider c)
    {
        //If we are out of hint zone, hide the level hint.
        if (_hintController != null && c.CompareTag("HintZone"))
        {
            _hintController.hideHint();
        }
    }

    private void OnTriggerEnter (Collider c)
	{
        //If we are in hint zone, show the level hint.
        if (_hintController != null && c.CompareTag("HintZone") && _torchesLighted < _torchesToLightToOpenDoor)
        {
            _hintController.showHint();
        }

		//Collider with "Is Trigger" option checked.
		if (c.CompareTag("Door") && levelComplete()) {
			Loadlevel();
		}
		
		if (!c.CompareTag("LightStickOpenDoor")) return;
		
		/*
		 * Set private vars: _torchTriggerEntered and _tController 
		 * Used in LightTorch and TorchControllerLight methods.
		 */
		_torchTriggerEntered = true;
		_torchController = c.GetComponent<TorchController> ();
	}

	private void LightTorch ()
	{
		/*
		//Check: 
		- torchTriggerEntered is true, 
		- tController is not null 
		- space key is pressed
		- tController is lighting
		*/
		if (!_torchTriggerEntered || !_torchController || !Input.GetKeyDown(KeyCode.Space) || _torchController.GetIsLighting()) return;
		
		_animator.SetTrigger (LightTorchAnimation);
		
		StartCoroutine(TorchControllerLight());
		
	}
	
   //Play Fire Ignite sound
	private void FireIgnite()
	{
		_audioFireIgnite.Play();
	}

	private IEnumerator TorchControllerLight ()
	{

        _isLightingAction = true;

        //Wait 1.5 seconds to light the torch.
        yield return new WaitForSeconds(1.5f);

        //Light the torch and increment torchesLighted var.
        _torchController.Lighting ();
		_torchesLighted++;

        //Register from this level wich torch
        _analyticsController.TorchLighted(_torchController.torchId);


        //Reset torchTriggerEntered and tController vars.
        _torchTriggerEntered = false;
		_torchController = null;
        
        _isLightingAction = false;

        if (!levelComplete()) yield break;
		_cameraController.SetTarget(_cameraController.exitDoor);
		_doorAnimator.SetBool (Open, true);
		_audioOpenDoor.Play();
		StartCoroutine(FocusToPlayer());
	}

	private IEnumerator FocusToPlayer()
	{
		yield return new WaitForSeconds(1.5f);
		_cameraController.SetTarget(_cameraController.player);
	}

    public int getTorchesToLightToOpenDoor() {
        return _torchesToLightToOpenDoor;
    }

    public bool levelComplete() {
        return _torchesToLightToOpenDoor == _torchesLighted;
    }


    /*void Idle(){
		string idle;
		
		if (Time.fixedTime % waitToIdle == 0 && !moving) {
			idle = Random.Range(0, 2) == 0 ? "Idle1" : "Idle2" ; 
			animator.Play(idle);			
		}
	}*/


}
