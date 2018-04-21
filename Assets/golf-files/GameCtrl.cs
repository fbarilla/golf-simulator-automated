using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using UnityEditor;
using System.IO;
using System.Text;


public class GameCtrl : MonoBehaviour {

	public Transform holeObj;
	public holeCtrl holeScript;

	// ball positions 
	private float[] pos_1 = { 7.332f, -0.192f, -1.554f};
	private float[] pos_2 = { 7.332f, -0.137f, 0.159f};
	private float[] pos_3 = { 6.658f, -0.272f, -2.489f};
	private float[] pos;

	//Use to switch between Force Modes
	enum ModeSwitching { Start, Force};
	ModeSwitching m_ModeSwitching;

	Rigidbody m_Rigidbody;
	Vector3 vForce;
	private bool isGameOver = false;
	private bool hasWon = false;
//	private string m_AccelString;
//	private string m_AngleString;
	private string m_DistanceString;
	private float m_Accel, m_Angle;
	private float m_InitialDistance, m_InitialAngle;
	private Vector3 m_StartPos; 
	private float mass = 2.0f;
	private float speedFactor = 10.0f;
	private int frame = 0;
	private float drag = 0.0f;
	private string input_data = "/tmp/unity_data.txt";
	private string output_results = "/tmp/unity_results.txt";
	private string[] strArr;
	private bool lerp = true;
	private int m_BallPos;

	private float dragFactor = 0.05f;

	private float lerpTime =5.0f;           // original: 2.0f
	private float accelFactor = 15.0f;     // original: 25.0f
	private float dragTrigger = 0.995f;
	float currentLerpTime = 0;
	private string previousData = "";
	private int m_Id;

	public static int m_HolePos = 1;
	private Renderer m_Renderer;

	// Use this for initialization
	void Start () {

		// get the Rigidbody component you attach to the GameObject
		m_Rigidbody = GetComponent<Rigidbody>();
		// get the renderer attached to the GameObject
		m_Renderer = GetComponent<Renderer>();
		// hide the ball unitl it's positioned
		m_Renderer.enabled = false;
		//The forces typed in from the text fields (the ones you can manipulate in Game view)
//		m_AccelString = "15";
//		m_AngleString = "0";
//		//The GameObject's starting position and Rigidbody position
//		m_StartPos = transform.position;
//		// compute initial distance
//		m_InitialDistance = Vector3.Distance (transform.position, holeObj.transform.position);
//		//Debug.Log ("Initial Distance: " + m_InitialDistance);
//		// compute angle to the hole
//		// sin(a) = holeObj.transform.position.z - transform.position.z / m_InitialDistance;
//		m_InitialAngle = Mathf.Rad2Deg * Mathf.Asin((holeObj.transform.position.z - transform.position.z) / m_InitialDistance);
//		//Debug.Log ("Angle: " + m_InitialAngle);
//		// freeze the ball
//		m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;

		// check the input parameters
		// ReadInputDataFile ();
		StartCoroutine(CheckInputFile());

	}


	// Update is called once per frame
	void FixedUpdate () {
		// switching mode 
		switch (m_ModeSwitching) {
		//This is the starting mode which resets the GameObject
		case ModeSwitching.Start:
			// reset flag
			hasWon = false;
			// reset frame
			frame = 0;
			// reset speed factor
			speedFactor = 10.0f;
			// reset drag
			drag = 0.0f;
			// reset the flags and variables
			isGameOver = false;
			currentLerpTime = 0;
			// reset the angle
			m_InitialAngle = Mathf.Rad2Deg * Mathf.Asin((holeObj.transform.position.z - transform.position.z) / m_InitialDistance);
			// reset the distance
			m_DistanceString = "-1";
			// reset the mass and the drag
			m_Rigidbody.mass = mass;
			m_Rigidbody.drag = 0.0f;
			// enable the collider after a restart
			this.GetComponent<Collider>().enabled = true;
			//This resets the GameObject and Rigidbody to their starting positions
			transform.position = m_StartPos;
			//This resets the velocity of the Rigidbody
			m_Rigidbody.velocity = new Vector3 (0f, 0f, 0f);
			break;

			//This is Force Mode, using a continuous force on the Rigidbody considering its mass
		case ModeSwitching.Force:
			// remove the contstraint (stop)
			m_Rigidbody.constraints = RigidbodyConstraints.None;
			// compute force to apply based on the angle
			vForce = Quaternion.AngleAxis (m_InitialAngle + m_Angle, Vector3.up) * -Vector3.right;
			//Debug.Log ("vForce: " + vForce.ToString ("F2"));
			if (!lerp) {
				if (frame <= m_Accel) {
					//accelerate
					m_Rigidbody.AddForce (vForce * speedFactor, ForceMode.Acceleration);
					frame++;
				} else {
					// apply force
					m_Rigidbody.AddForce (vForce * speedFactor, ForceMode.Force);
					//increase the drag
					drag += dragFactor;
					//Debug.Log("Drag: " + drag);
					// m_DragString = drag.ToString ("F2");
					m_Rigidbody.drag = drag;
					// Debug.Log ("Velocity: " + m_Rigidbody.velocity);
					if (Mathf.Abs (m_Rigidbody.velocity.x) < 0.2) {
						if (!hasWon) {
							//stop the ball
							m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
							// set the flag
							isGameOver = true;
						}
					}
				}
			} else {
				currentLerpTime += Time.deltaTime;
				if (currentLerpTime > lerpTime) {
					currentLerpTime = lerpTime;
				}
				// acceleration/deceleration function (sigmoid)
				float t = currentLerpTime / lerpTime;
				t = t * t * (3f - 2f * t);
				// Debug.Log ("t: " + t);
				m_Rigidbody.AddForce (vForce * (1.0f - t) * accelFactor, ForceMode.Force);

				// almost stopped, apply drag
				if (t > dragTrigger) {
					drag += dragFactor;
					m_Rigidbody.drag = drag;

					if (Mathf.Abs (m_Rigidbody.velocity.x) < 0.05) {
						if (!hasWon) {
							//stop the ball
							m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
							// set the flag
							isGameOver = true;
						}
					}
				}

			}
			//Debug.Log ("Velocity: " + m_Rigidbody.velocity);
			break;
		}

		// test if the ball has fallen 
		if (m_Rigidbody.position.y < -5) {

			if (hasWon == false) {
				//Debug.Log ("Ball has fallen outside the green....");
				m_DistanceString = "-1";
				string results = m_Id + ", " + m_DistanceString;
				// Write Results 
				WriteResults(results);
			}
			// restart
			//StartCoroutine(Reset());
		}


		if (isGameOver ) {
			if (!hasWon) {
				// compute the distance to the hole
				float dist = Vector3.Distance (transform.position, holeObj.transform.position);
				m_DistanceString = dist.ToString ("F2");
				// calculate the relative position to thehole
				Vector3 relativeDistance = holeObj.InverseTransformPoint(transform.position);
				string results = m_Id + ", " + m_DistanceString + ", " + relativeDistance.ToString("F2");
				// Write Results 
				WriteResults(results);
				// restart
				//StartCoroutine(Reset());
			}
		}

	}

	void OnTriggerEnter(Collider other)
	{
		if (other.name == "hole") {
			m_DistanceString = "0";
			// stop the object
			//m_Rigidbody.velocity = new Vector3 (0f, 0f, 0f);
			// increase the mass to fall sharply
			m_Rigidbody.mass = 100;
			// fall
			this.GetComponent<Collider>().enabled = false;
			// set flag
			hasWon = true;
			string results = m_Id + ", " + m_DistanceString;
			// Write Results 
			WriteResults(results);

			// restart
			//StartCoroutine(Reset());

		}
	}

	IEnumerator Reset() {
		yield return new WaitForSeconds(1.0f);
		m_ModeSwitching = ModeSwitching.Start;
		StartCoroutine(Restart());
	}

	IEnumerator Restart() {
		yield return new WaitForSeconds(1.0f);
		m_ModeSwitching = ModeSwitching.Force;
	}

	void WriteResults(string results) {
		byte[] myData = System.Text.Encoding.UTF8.GetBytes(results);
		File.WriteAllBytes (output_results, myData);
	}


	IEnumerator CheckInputFile() {
		float freq = 2.0f;  
		float timer = 0;
		while (true) {
			byte[] readData;
			readData = File.ReadAllBytes (input_data);
			string str = System.Text.Encoding.UTF8.GetString (readData);
			if (str != previousData) {
				previousData = str;
				// load the new data
				strArr = str.Split (new char[] {','});
				m_Id = int.Parse (strArr [0]);
				m_HolePos = int.Parse (strArr [1]);
				m_BallPos = int.Parse (strArr [2]);
				accelFactor = float.Parse(strArr [3]);
				m_Angle = float.Parse(strArr [4]);

				// position the ball
				//select 
				if (m_BallPos == 1) {
					pos = pos_1;
				} else if (m_BallPos == 2) {
					pos = pos_2;
				} else if (m_BallPos == 3) {
					pos = pos_3;
				}
				//Debug.Log ("Ball Position: " + m_BallPos);
				Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
				//Debug.Log ("Position: " + hole_pos.ToString("F3"));
				transform.position = hole_pos;

				//The GameObject's starting position and Rigidbody position
				m_StartPos = transform.position;
				// compute initial distance
				m_InitialDistance = Vector3.Distance (transform.position, holeObj.transform.position);
				//Debug.Log ("Initial Distance: " + m_InitialDistance);
				// compute angle to the hole
				// sin(a) = holeObj.transform.position.z - transform.position.z / m_InitialDistance;
				m_InitialAngle = Mathf.Rad2Deg * Mathf.Asin((holeObj.transform.position.z - transform.position.z) / m_InitialDistance);
				//Debug.Log ("Angle: " + m_InitialAngle);
				// freeze the ball
				m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
				// display the ball 
				m_Renderer.enabled = true;

				// restart the game
				// Debug.Log ("Restart Game....");
				StartCoroutine(Reset());

			} else {
				//Debug.Log ("No data change....");
			}

			while (timer < freq) {
				timer += Time.deltaTime;
				yield return null;
			}
			timer = 0f;
			yield return null;
		}	
	}

}
