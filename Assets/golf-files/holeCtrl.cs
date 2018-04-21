using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using UnityEditor;
using System.IO;
using System.Text;

public class holeCtrl : MonoBehaviour {

	private float[] pos_1 = { 0.0f, -0.624f, 1.08f, 5.33f, -5.94f, -2.23f};  // Position, Rotation (3 val each)
	private float[] pos_2 = { 0.0f, -0.715f, 3.49f, -3.0f, -5.5f, -2.24f}; 
	private float[] pos_3 = { 1.52f, -0.29f, -2.32f, 3.44f, -5.5f, -2.24f}; 
	private float[] pos;
	private string input_data = "/tmp/unity_data.txt";
	private string previousData = "";
	private int m_Id;
	private string[] strArr;
	private int m_HolePos;
	private Renderer m_Renderer;

	// Use this for initialization
	void Start () {

		// get the renderer attached to the GameObject
		m_Renderer = GetComponent<Renderer>();
		// hide the hole until it's positioned
		m_Renderer.enabled = false;

		// check the input parameters
		StartCoroutine(CheckInputFile());

//		Debug.Log ("Hole Pos: " + GameCtrl.m_HolePos);
//		int nb = GameCtrl.m_HolePos; 

//		//select 
//		if (nb == 1) {
//			pos = pos_1;
//		} else if (nb == 2) {
//			pos = pos_2;
//		} else {
//			pos = pos_3;
//		}

//		// position the hole
//		Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
//		transform.position = hole_pos;
//		Vector3 hole_rot = new Vector3(pos[3], pos[4], pos[5]);
//		transform.rotation = Quaternion.Euler(hole_rot);
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

//	public void positionHole(int holePos) {
//		Debug.Log ("Hole Pos: " + holePos);
//		int nb = holePos; 

//		//select 
//		if (nb == 1) {
//			pos = pos_1;
//		} else if (nb == 2) {
//			pos = pos_2;
//		} else {
//			pos = pos_3;
//		}

//		// position the hole
//		Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
//		transform.position = hole_pos;
//		Vector3 hole_rot = new Vector3(pos[3], pos[4], pos[5]);
//		transform.rotation = Quaternion.Euler(hole_rot);

//	}

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

				// position the hole
				//select 
				if (m_HolePos == 1) {
					pos = pos_1;
				} else if (m_HolePos == 2) {
					pos = pos_2;
				} else if (m_HolePos == 3) {
					pos = pos_3;
				}

				//Debug.Log("Hole Position: " + m_HolePos);
				Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
				//Debug.Log ("Position: " + hole_pos.ToString("F3"));
				transform.position = hole_pos;
				Vector3 hole_rot = new Vector3(pos[3], pos[4], pos[5]);
				transform.rotation = Quaternion.Euler(hole_rot);

				// display the ball 
				m_Renderer.enabled = true;

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
