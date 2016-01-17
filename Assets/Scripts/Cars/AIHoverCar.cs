﻿using UnityEngine;
using System.Collections;
using InControl;

public class AIHoverCar : CarMetrics {

	Rigidbody m_body;
	protected float bullDozeStrength = 1000f;
	protected float accelerateStrength = 30000f;
	protected float turnStrength = 5000f;
	protected float strafeStrength = 10000f;
	protected float verticalAxisDirection = 1f;
	protected float horizontalAxisDirection = 1f;
	protected float strafeAxisDirection = 1f;
	protected float m_hoverForce = 9.0f;
	protected float m_hoverHeight = 2.0f;
	public GameObject[] m_hoverPoints;
	protected float faceObjectBuffer = 50f;
	float accelerationModifier;

	//FX
	[SerializeField] GameObject smoke, spark, deathExplosion;

	public GameObject currentTarget;

	bool accelerate, steer, strafe;

	int m_layerMask;
	int m_wallMask;
	bool isTouchingGround;
	
	void Start()
	{
		m_body = GetComponent<Rigidbody>();
		m_layerMask = 1 << LayerMask.NameToLayer("Characters");
		m_layerMask = ~m_layerMask;

		m_wallMask = 1 << LayerMask.NameToLayer("Walls");
	}
		
	void FixedUpdate()
	{
		if (accelerate) {
			Accelerate();
		}
		if (steer) {
			Steer ();
		}
		if (strafe) {
			Strafe ();
		}

		#region hoverphysics
		//HOVER PHYSICS
		RaycastHit hit;
		for (int i = 0; i < m_hoverPoints.Length; i++)
		{
			var hoverPoint = m_hoverPoints [i];
			if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out hit,m_hoverHeight, m_layerMask)) {
				m_body.AddForceAtPosition(Vector3.up * m_hoverForce * (1.0f - (hit.distance / m_hoverHeight)), hoverPoint.transform.position);
				isTouchingGround = true;
			}
			else {
				isTouchingGround = false;
				if (transform.position.y > hoverPoint.transform.position.y) {
					m_body.AddForceAtPosition(hoverPoint.transform.up * m_hoverForce, hoverPoint.transform.position);
				}
				else {
					m_body.AddForceAtPosition(hoverPoint.transform.up * -m_hoverForce, hoverPoint.transform.position);
				}
			}
		}
		#endregion
	}

	#region movementFunctions
	public void Accelerate() {
		m_body.AddForce(transform.forward * (accelerateStrength+accelerationModifier) * verticalAxisDirection);
	}

	IEnumerator SlowDown (float time) {
		accelerationModifier = -3 * accelerateStrength/4f;
		yield return new WaitForSeconds(time);
		accelerationModifier = 0;
	}
	
	public void Steer() {
		m_body.AddRelativeTorque(Vector3.up * horizontalAxisDirection * turnStrength);
	}

	public void Strafe() {
		m_body.AddForce(transform.right * strafeStrength * strafeAxisDirection);
	}

	public void SetThrust (bool on, bool directionForward) {
		if (on == false) {
			accelerate = false;
		}
		else {
			accelerate = true;
		}
		if (directionForward) {
			verticalAxisDirection = 1f;
		}
		else {
			verticalAxisDirection = -1f;
		}
	}

	public void SetSteering (bool on, bool directionRight) {
		if (on == false) {
			steer = false;
		}
		else {
			steer = true;
		}
		if (directionRight) {
			horizontalAxisDirection = -1f;
		}
		else {
			horizontalAxisDirection = 1f;
		}
	}

	public void SetStrafe (bool on, bool directionRight) {
		if (on == false) {
			strafe = false;
		}
		else {
			strafe = true;
		}
		if (directionRight) {
			strafeAxisDirection = -1f;
		}
		else {
			strafeAxisDirection = 1f;
		}
	
	}

	public bool IsTargetInFront (Vector3 target) {
		Vector3 targetDirection = target - transform.position;
		if (Vector3.Dot (transform.forward, targetDirection) > 0) {
			return true;
		}
		else {
			return false;
		}
	}

	public float IsTargetOnRightOrLeft (Vector3 target) {
		Vector3 relativePoint = transform.InverseTransformPoint(target);
		return relativePoint.x;
		//where positive means on right negative means on left
	}

	public float GetAngleToTarget(Vector3 target) {
		Vector3 targetDirection = target - transform.position;
		targetDirection = new Vector3(targetDirection.x, 0, targetDirection.z).normalized;
		return Mathf.Acos(Vector3.Dot (transform.forward,targetDirection));
		//always returns positive angle value in radians
	}

	public void FaceTarget (Vector3 target) {
		//face target 
		if (IsTargetOnRightOrLeft(target) < faceObjectBuffer && GetAngleToTarget(target) > 0.3f){
			SetSteering(true,true);
		}
		else if (IsTargetOnRightOrLeft(target) > faceObjectBuffer && GetAngleToTarget(target) > 0.3f){
			SetSteering(true,false);
		}
		else {
			SetSteering(false,false);
		}
	}

	public void MoveTowardTarget (Vector3 target) {
		//use x/z coordinates to tell where target is with regards to car
		if (Vector3.Distance(transform.position, target) > 10) {
			SetThrust(true, true);
		}
		else {
			SetThrust(false,false);
		}
	}
	public void ForceTowardTarget (Vector3 target) {
		//move directly toward without steering (for use when getting tractor beamed)

		if (Vector3.Distance(transform.position, target) > 1f) {
			print (target + "target");
			Vector3 directionTowardBulldozePosition = target-transform.position;
			Debug.DrawRay(transform.position,target,Color.blue);

			m_body.AddForce(new Vector3(directionTowardBulldozePosition.x,0f,directionTowardBulldozePosition.z)*bullDozeStrength);
		}
	}

	public void BackUpIntoTarget (Vector3 target) {
		if (Vector3.Distance(transform.position, target) > 10f) {
			SetThrust(true, false);
		}
		else {
			SetThrust(false,false);
		}
	}

	public void FaceAwayFromTarget(Vector3 target) {
		if (IsTargetOnRightOrLeft(target) < 0 && GetAngleToTarget(target) < 3f){
			SetSteering(true,false);
		}
		else if (IsTargetOnRightOrLeft(target) > 0 && GetAngleToTarget(target) < 3f){
			SetSteering(true,true);
		}
		else {
			SetSteering (false,false);
		}
	}
	#endregion

	#region OnCollision
	void OnCollisionEnter(Collision thisCollision) {
		if (thisCollision.collider.gameObject.tag == "Player") {
			if (PlayerCar.s_instance.isThrusting) {
				TakeHitFromThrust(thisCollision.contacts[0].point);
				TakeDamage(10f);
			}
		}
	}
	#endregion
	#region EventDrivenFunctions
	protected void OnEnable() {
		PlayerCar.BeginTarget+=BeginIsTargeted;
		PlayerCar.EndTarget+=EndIsTargeted;
		PlayerCar.PlayerInteractStart+=ReceiveInteract;
		PlayerCar.PlayerInteractEnd+=ReceiveDeinteract;
	}

	protected void OnDisable() {
		PlayerCar.BeginTarget-=BeginIsTargeted;
		PlayerCar.EndTarget-=EndIsTargeted;
		PlayerCar.PlayerInteractStart-=ReceiveInteract;
		PlayerCar.PlayerInteractEnd-=ReceiveDeinteract;
	}

	void ReceiveInteract () {
		isReceivingInteract = true;
	}

	void ReceiveDeinteract () {
		isReceivingInteract = false;

	}

	void BeginIsTargeted () {
		if (thisCarType == CarType.Mechanic) {
			
		}
	}

	void EndIsTargeted () {
		if (thisAIState == AIState.Submission) {
			
		}
	}

	#endregion
	void TakeHitFromThrust (Vector3 pointOfContact) {
		Instantiate(spark,pointOfContact,Quaternion.identity);
	}
	public override void Die () {
		if (thisAIState != AIState.Disabled) {
			Instantiate (deathExplosion, transform.position, Quaternion.identity);
			GameObject smokeObj = Instantiate (smoke, transform.position, Quaternion.identity)as GameObject;
			smokeObj.transform.SetParent (gameObject.transform);
			thisAIState = AIState.Disabled;
			foreach (Light x in tailLights) {
				x.enabled = false;
			}
			foreach (Light y in headLights) {
				y.enabled = false;
			}
		}
	}
	#region WanderStateFunctions
	void DetectForWalls () {
		//ray cast forward, right and left
		RaycastHit hitRight, hitLeft, hitForward;
		if (Physics.Raycast (transform.position, transform.forward, out hitForward, wallDetectionDistance, m_wallMask)) {
			isWallInFront = true;
		} else {
			isWallInFront = false;

		}
		if (Physics.Raycast (transform.position, transform.right, out hitRight, wallDetectionDistanceLateral, m_wallMask)) {
			isWallOnRight = true;
		} else {
			isWallOnRight = false;

		}
		if (Physics.Raycast (transform.position, -transform.right, out hitLeft, wallDetectionDistanceLateral, m_wallMask)) {
			isWallOnLeft = true;
		} else {
			isWallOnLeft = false;
		}

	}

	bool GenerateRandomBool() {
		return (Random.value > 0.5f);
	}

	void SteerAwayFromWalls() {
		if (!isWallInFront) {
			Accelerate ();
		}

		if (isWallOnLeft && isWallOnRight) {

		} else if (isWallOnLeft || isWallInFront) {
			SetSteering (true, false);
		} else if (isWallOnRight) {
			SetSteering (true, true);

		} else {
			SetSteering (false, false);
		}
	}

	void SetTurnByAngle() {
		if (Random.Range(0, randomTurnProbability) == 0 && !isSteering){
			angleToRotateBy = Random.Range (10, 179);
			isTurningRight = GenerateRandomBool ();
			previousForwardDirection = transform.forward;
			isSteering = true;
			print ("RANDOM TURN");
		}
	}

	void TurnToCompletion() {
		if (Vector3.Angle (previousForwardDirection, transform.forward) > angleToRotateBy) {
			print ("TURN COMPLETE");
			isSteering = false;
			SetSteering (false, false);
		} else {
			print ("STEERING");
			SetSteering (true, isTurningRight);
			Accelerate ();
		}
	}
	#endregion
	//____________________________________________________STATE MACHINE____________________________________________________

	#region StateMachine
	public enum AIState {Idle, Face, Submission, Flee, Wander, Suicide, Follow, Court, Chase, Disabled, Report, Explore, Survive, Hunt}; 
	public enum CarType {Thief, Mechanic, Civ};
	public CarType thisCarType;
	public AIState thisAIState = AIState.Idle;
	bool isReceivingInteract;
	bool isWallInFront, isWallOnRight, isWallOnLeft;

	//rotate by angle logic
	Vector3 previousForwardDirection;
	float angleToRotateBy;
	bool isSteering;
	bool isTurningRight;
	int randomTurnProbability = 300;
	private float wallDetectionDistance = 20f, wallDetectionDistanceLateral = 5f;
	void Update()
	{
		switch (thisAIState) {
		case AIState.Disabled :
			if (isReceivingInteract) {
				ForceTowardTarget(PlayerCar.s_instance.bulldozeChildTransform.position);
				FaceTarget(PlayerCar.s_instance.bulldozeChildTransform.GetChild (0).position);
			}
			break;
		case AIState.Idle :
			break;
		case AIState.Face :
			FaceTarget(PlayerCar.s_instance.transform.position);
			break;
		case AIState.Chase :
			MoveTowardTarget(PlayerCar.s_instance.transform.position);
			FaceTarget(PlayerCar.s_instance.transform.position);
			break;
		case AIState.Court :
			FaceAwayFromTarget(PlayerCar.s_instance.transform.position);
			break;

		case AIState.Submission :
			FaceAwayFromTarget(PlayerCar.s_instance.transform.position);
			BackUpIntoTarget(PlayerCar.s_instance.transform.position);

			break;

		case AIState.Wander: 
			SetTurnByAngle ();
			if (isSteering) {
				TurnToCompletion ();
			}
			DetectForWalls ();
			if (!isSteering) {
				SteerAwayFromWalls ();
			}
			break;

		}
	}
	#endregion
}
