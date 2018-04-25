using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class PlayerCtrl : MonoBehaviour {

	public Transform holeObj;
	// public holeCtrl holeScript;

	// ball positions 
	private float[] pos_1 = { 7.332f, -0.192f, -1.554f};
	private float[] pos_2 = { 7.332f, -0.137f, 0.159f};
	private float[] pos_3 = { 6.658f, -0.272f, -2.489f};
	private float[] pos;

	//Use to switch between Force Modes
	enum ModeSwitching { Start, Force, Result, Idle};
	ModeSwitching m_ModeSwitching;

	Rigidbody m_Rigidbody;
	Vector3 vForce;
	//private bool isGameOver = false;
	private bool hasWon = false;
	private string m_DistanceString;
	//private float m_Accel = 150;
	private float m_Angle;
	private float m_InitialDistance, m_InitialAngle;
	private Vector3 m_StartPos; 
	private float mass = 2.0f;
	private float drag = 0.0f;
	private string input_data = "/tmp/unity_data.txt";
	private string output_results = "/tmp/unity_results.txt";
	private string[] strArr;
	private int m_BallPos;
	private string results;

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
		// wait for new data to be sent by external python process
		m_ModeSwitching = ModeSwitching.Idle;
	}


	// Update is called once per frame
	void FixedUpdate () {
		// switching mode 
		switch (m_ModeSwitching) {
			//This is the starting mode which resets the GameObject
			case ModeSwitching.Start:
				// reset flag
				hasWon = false;
				// reset drag
				drag = 0.0f;
				// reset the flags and variables
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

			//This is Force Mode
			case ModeSwitching.Force:
				// remove the contstraint (stop)
				m_Rigidbody.constraints = RigidbodyConstraints.None;
				// compute force to apply based on the angle
				vForce = Quaternion.AngleAxis (m_InitialAngle + m_Angle, Vector3.up) * -Vector3.right;
				//Debug.Log ("vForce: " + vForce.ToString ("F2"));

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
							results = "-99";
							// switch to results
							m_ModeSwitching = ModeSwitching.Result;
						}
					}
				}

				// test if the ball has fallen 
				if (m_Rigidbody.position.y < -5) {

					if (hasWon == false) {
						//Debug.Log ("Ball has fallen outside the green....");
						results = "-1";
						//stop the ball
						m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
						// switch to results
						m_ModeSwitching = ModeSwitching.Result;
					}
				}
				break;

			case ModeSwitching.Result:
				if (results.Equals ("-1")||results.Equals ("0")) {
					// nothing else to do. Ball is in the hole or fell outside of the green
				} else {
					float dist = Vector3.Distance (transform.position, holeObj.transform.position);
					m_DistanceString = dist.ToString ("F2");
					// calculate the relative position to thehole
					Vector3 relativeDistance = holeObj.InverseTransformPoint(transform.position);
					results = m_DistanceString + ", " + relativeDistance.ToString("F2");
				}
				// write the results
				byte[] myData = System.Text.Encoding.UTF8.GetBytes(m_Id + ", " + results);
				File.WriteAllBytes (output_results, myData);
				// go to idle state
				m_ModeSwitching = ModeSwitching.Idle;
				break;

			case ModeSwitching.Idle:
				// results have been written. 
				// wait for the expternal python process to read the result file and generate new set of data
				StartCoroutine(WaitForSeconds());
				// get new set of data
				getNextData();
				break;
		}

	}

	void OnTriggerEnter(Collider other)
	{
		if (other.name == "hole") {
			// Debug.Log ("Collision....");
			results = "0";
			// increase the mass to fall sharply
			m_Rigidbody.mass = 10;
			// fall
			this.GetComponent<Collider>().enabled = false;
			// set the flag
			hasWon = true;
			// switch to results
			m_ModeSwitching = ModeSwitching.Result;

		}
	}

	IEnumerator WaitForSeconds() {
		yield return new WaitForSeconds(5.0f);
	}

	IEnumerator Reset() {
		yield return new WaitForSeconds(0.2f);
		m_ModeSwitching = ModeSwitching.Start;
		StartCoroutine(Restart());
	}

	IEnumerator Restart() {
		yield return new WaitForSeconds(0.2f);
		m_ModeSwitching = ModeSwitching.Force;
	}

	void getNextData() {
		byte[] readData;
		if (File.Exists (input_data)) {  
			readData = File.ReadAllBytes (input_data);
			string str = System.Text.Encoding.UTF8.GetString (readData);
			if (str != previousData) {
				previousData = str;
				// load the new data
				strArr = str.Split (new char[] { ',' });
				m_Id = int.Parse (strArr [0]);
				m_HolePos = int.Parse (strArr [1]);
				m_BallPos = int.Parse (strArr [2]);
				accelFactor = float.Parse (strArr [3]) / 10;
				m_Angle = float.Parse (strArr [4]);

				Debug.Log ("Data: " + str);

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
				Vector3 ball_pos = new Vector3 (pos [0], pos [1], pos [2]);
				//Debug.Log ("Position: " + hole_pos.ToString("F3"));
				transform.position = ball_pos;

				//The GameObject's starting position and Rigidbody position
				m_StartPos = transform.position;
				// compute initial distance
				m_InitialDistance = Vector3.Distance (transform.position, holeObj.transform.position);
				//Debug.Log ("Initial Distance: " + m_InitialDistance);
				// compute angle to the hole
				// sin(a) = holeObj.transform.position.z - transform.position.z / m_InitialDistance;
				m_InitialAngle = Mathf.Rad2Deg * Mathf.Asin ((holeObj.transform.position.z - transform.position.z) / m_InitialDistance);
				//Debug.Log ("Angle: " + m_InitialAngle);
				// freeze the ball
				m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
				// display the ball 
				m_Renderer.enabled = true;

				// restart the game
				// Debug.Log ("Restart Game....");
				StartCoroutine (Reset ());

			} else {
				//Debug.Log ("No data change....");
			}
		}
	}

}
