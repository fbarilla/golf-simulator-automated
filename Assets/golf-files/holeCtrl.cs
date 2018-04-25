using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class holeCtrl : MonoBehaviour {

	private float[] pos_1 = { 0.0f, -0.624f, 1.08f, 5.33f, -5.94f, -2.23f};  // Position, Rotation (3 val each)
	private float[] pos_2 = { 0.0f, -0.715f, 3.49f, -3.0f, -5.5f, -2.24f}; 
	private float[] pos_3 = { 1.52f, -0.29f, -2.32f, 3.44f, -5.5f, -2.24f}; 
	private float[] pos;
	private int m_HolePos;
	private Renderer m_Renderer;
	private int previousHolePos;
	private int currentHolePos;

	// Use this for initialization
	void Start () {

		// get the renderer attached to the GameObject
		m_Renderer = GetComponent<Renderer>();

		// hide the hole until it's positioned
		m_Renderer.enabled = false;

		// hole position
		m_HolePos = PlayerCtrl.m_HolePos;

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

		// hole location
		previousHolePos = m_HolePos;
		// Debug.Log("Hole Location: " + GameCtrl.m_HolePos);

	}

	// Update is called once per frame
	void Update () {

		currentHolePos = PlayerCtrl.m_HolePos;
		if (currentHolePos != previousHolePos) {
			//select 
			if (currentHolePos == 1) {
				pos = pos_1;
			} else if (currentHolePos == 2) {
				pos = pos_2;
			} else if (currentHolePos == 3) {
				pos = pos_3;
			}

			//Debug.Log("Hole Position: " + m_HolePos);
			Vector3 hole_pos = new Vector3(pos[0], pos[1], pos[2]);
			//Debug.Log ("Position: " + hole_pos.ToString("F3"));
			transform.position = hole_pos;
			Vector3 hole_rot = new Vector3(pos[3], pos[4], pos[5]);
			transform.rotation = Quaternion.Euler(hole_rot);

			previousHolePos = currentHolePos;

		}
		
	}

}
