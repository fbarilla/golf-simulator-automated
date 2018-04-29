using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

public class PlayerCtrl : MonoBehaviour {

	private bool manual = true;

	public Transform holeObj;

	// ball positions 
	private float[] ball_pos_1 = { 7.332f, -0.192f, -1.554f};
	private float[] ball_pos_2 = { 7.332f, -0.146f, 0.159f};
	private float[] ball_pos_3 = { 6.658f, -0.25f, -2.489f};
	private float[] pos;

	private float[] hole_pos_1 = { 0.0f, -0.624f, 1.08f, 5.33f, -5.94f, -2.23f};  // Position, Rotation (3 val each)
	private float[] hole_pos_2 = { 0.0f, -0.715f, 3.49f, -3.0f, -5.5f, -2.24f}; 
	private float[] hole_pos_3 = { 1.52f, -0.29f, -2.32f, 3.44f, -5.5f, -2.24f}; 

	//Use to switch between Force Modes
	enum ModeSwitching { Start, Force, Result, Idle, Manual};
	ModeSwitching m_ModeSwitching;

	Rigidbody m_Rigidbody;
	Vector3 vForce;
	private bool hasWon = false;
	private string m_DistanceString = "0";
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
	private string m_AccelFactorString = "150";
	private string m_AngleString = "0";
	private string m_HolePosString = "1";
	private string m_BallPosString = "1";

	private float dragFactor = 0.05f;

	private float lerpTime = 5.0f;           // original: 2.0f
	private float m_AccelFactor = 15.0f;     // original: 25.0f
	private float dragTrigger = 0.995f;
	float currentLerpTime = 0;
	private string previousData = "";
	private int m_Id;

	public static int m_HolePos = 1;
	private Renderer m_Renderer;
	private int cnt;

	void Awake() {
		Application.logMessageReceived += HandleException;
	}

	// Use this for initialization
	void Start () {
		// set input variables
		m_AccelFactorString = "150";
		m_AngleString = "0";
		m_DistanceString = "0";
		m_HolePosString = "1";
		m_BallPosString = "1";
		// get the Rigidbody component you attach to the GameObject
		m_Rigidbody = GetComponent<Rigidbody>();
		// get the renderer attached to the GameObject
		m_Renderer = GetComponent<Renderer>();
		// hide the ball unitl it's positioned
		m_Renderer.enabled = false;
		// check fps
		// Debug.Log("FPS: " + 1.0f / Time.deltaTime);

		// wait for new data to be sent by external python process
		m_ModeSwitching = ModeSwitching.Idle;

		// start the polling process
		StartCoroutine (CheckInputFile ());

		if (manual) {
			m_ModeSwitching = ModeSwitching.Manual;
		}
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

				// start after the variables have been set
				m_ModeSwitching = ModeSwitching.Force;
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
				m_Rigidbody.AddForce (vForce * (1.0f - t) * m_AccelFactor, ForceMode.Force);
	
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
				if (m_Rigidbody.position.y < -3) {

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
				// waiting zone
				break;

			case ModeSwitching.Manual:
				// reset distance
				m_DistanceString = "0";
				
				// parse the input values
				m_AccelFactor = float.Parse (m_AccelFactorString)/10;
				m_Angle = float.Parse (m_AngleString);
				m_HolePos = int.Parse (m_HolePosString);
				m_BallPos = int.Parse (m_BallPosString);

				// position the ball	
				//select
				if (m_BallPos == 1) {
					pos = ball_pos_1;
				} else if (m_BallPos == 2) {
					pos = ball_pos_2;
				} else if (m_BallPos == 3) {
					pos = ball_pos_3;
				}
				//Debug.Log ("Ball Position: " + m_BallPos);
				Vector3 ball_pos = new Vector3 (pos [0], pos [1], pos [2]);
				//Debug.Log ("Position: " + hole_pos.ToString("F3"));
				transform.position = ball_pos;
				//The GameObject's starting position and Rigidbody position
				m_StartPos = transform.position;

				// position the hole
				//select 
				if (m_HolePos == 1) {
					pos = hole_pos_1;
				} else if (m_HolePos == 2) {
					pos = hole_pos_2;
				} else if (m_HolePos == 3) {
					pos = hole_pos_3;
				}

				//Debug.Log("Hole Position: " + m_HolePos);
				Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
				//Debug.Log ("Position: " + hole_pos.ToString("F3"));
				holeObj.transform.position = hole_pos;
				Vector3 hole_rot = new Vector3(pos[3], pos[4], pos[5]);
				holeObj.transform.rotation = Quaternion.Euler(hole_rot);

				// compute initial distance
				m_InitialDistance = Vector3.Distance (transform.position, holeObj.transform.position);
				// compute angle to the hole
				// sin(a) = holeObj.transform.position.z - transform.position.z / m_InitialDistance;
				m_InitialAngle = Mathf.Rad2Deg * Mathf.Asin ((holeObj.transform.position.z - transform.position.z) / m_InitialDistance);
				// Debug.Log ("Angle: " + m_InitialAngle);
				// freeze the ball
				m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
				// display the ball
				m_Renderer.enabled = true;
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

	IEnumerator CheckInputFile() {
		float freq = 2.0f;
		float timer = 0;
		while (true) {
			byte[] readData;
			if(File.Exists (input_data)){
				readData = File.ReadAllBytes (input_data);
				string str = System.Text.Encoding.UTF8.GetString (readData);

				if (str.Length != 0) {
					cnt = str.Split (new char[] { ',' }).Length - 1;
				}
				// check if the data line has been fully written (4 commas )
				if (cnt == 4) {
					if (str != previousData) {
						previousData = str;
						// load the new data
						strArr = str.Split (new char[] { ',' });
						m_Id = int.Parse (strArr [0]);
						m_HolePos = int.Parse (strArr [1]);
						m_BallPos = int.Parse (strArr [2]);
						m_AccelFactor = float.Parse (strArr [3]) / 10;
						m_Angle = float.Parse (strArr [4]);

						Debug.Log ("Data: " + str);

						// position the ball
						//select
						if (m_BallPos == 1) {
							pos = ball_pos_1;
						} else if (m_BallPos == 2) {
							pos = ball_pos_2;
						} else if (m_BallPos == 3) {
							pos = ball_pos_3;
						}
						//Debug.Log ("Ball Position: " + m_BallPos);
						Vector3 ball_pos = new Vector3 (pos [0], pos [1], pos [2]);
						//Debug.Log ("Position: " + hole_pos.ToString("F3"));
						transform.position = ball_pos;
						//The GameObject's starting position and Rigidbody position
						m_StartPos = transform.position;

						// position the hole
						//select 
						if (m_HolePos == 1) {
							pos = hole_pos_1;
						} else if (m_HolePos == 2) {
							pos = hole_pos_2;
						} else if (m_HolePos == 3) {
							pos = hole_pos_3;
						}

						//Debug.Log("Hole Position: " + m_HolePos);
						Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
						//Debug.Log ("Position: " + hole_pos.ToString("F3"));
						holeObj.transform.position = hole_pos;
						Vector3 hole_rot = new Vector3(pos[3], pos[4], pos[5]);
						holeObj.transform.rotation = Quaternion.Euler(hole_rot);

						// compute initial distance
						m_InitialDistance = Vector3.Distance (transform.position, holeObj.transform.position);
						// compute angle to the hole
						// sin(a) = holeObj.transform.position.z - transform.position.z / m_InitialDistance;
						m_InitialAngle = Mathf.Rad2Deg * Mathf.Asin ((holeObj.transform.position.z - transform.position.z) / m_InitialDistance);
						// Debug.Log ("Angle: " + m_InitialAngle);
						// freeze the ball
						m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
						// display the ball
						m_Renderer.enabled = true;

						// restart the game
						// Debug.Log ("Restart Game....");
						if (m_ModeSwitching == ModeSwitching.Idle) {
							m_ModeSwitching = ModeSwitching.Start;
						}

					} else {
						//Debug.Log ("No data change....");
					}
				}
			}

			while (timer < freq) {
				timer += Time.deltaTime;
				yield return null;
			}
			timer = 0f;
			yield return null;
		}

	}

	void HandleException(string condition, string stackTrace, LogType type) {
		if (type == LogType.Exception) {
			m_ModeSwitching = ModeSwitching.Idle;
		}
	}

	//The function outputs buttons, text fields, and other interactable UI elements to the Scene in Game view
	void OnGUI()
	{
		if (manual) {
			//Getting the inputs from each text field and storing them as strings
			GUI.Label (new Rect (15, 75, 50, 20), "Accel");
			GUI.Label (new Rect (15, 105, 50, 20), "Angle");
			GUI.Label (new Rect (15, 135, 50, 20), "Ball position");
			GUI.Label (new Rect (15, 165, 50, 20), "Hole position");
			GUI.Label (new Rect (15, 195, 50, 20), "Distance");
			m_AccelFactorString = GUI.TextField (new Rect (100, 75, 50, 20), m_AccelFactorString, 25);
			m_AngleString = GUI.TextField (new Rect (100, 105, 50, 20), m_AngleString, 25);
			m_BallPosString = GUI.TextField (new Rect (100, 135, 50, 20), m_BallPosString, 25);
			m_HolePosString = GUI.TextField (new Rect (100, 165, 50, 20), m_HolePosString, 25);
			m_DistanceString = GUI.TextField (new Rect (100, 195, 50, 20), m_DistanceString, 25);

			//Press the button to reset the GameObject and Rigidbody
			if (GUI.Button (new Rect (10, 5, 150, 30), "Reset")) {
				//This switches to the start/reset case
				m_ModeSwitching = ModeSwitching.Manual;
			}

			//If you press the Start Button, switch to Force state
			if (GUI.Button (new Rect (10, 40, 150, 30), "Start")) {
				// remove the contstraint (stop)
				m_Rigidbody.constraints = RigidbodyConstraints.None;
				//Switch to Force (apply force to GameObject)
				m_ModeSwitching = ModeSwitching.Start;
			}

		}
	}
}
