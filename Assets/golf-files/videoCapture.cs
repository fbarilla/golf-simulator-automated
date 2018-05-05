using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System;
using System.Threading;

public class videoCapture : MonoBehaviour {

	[Range(1, 120)]
	public int frameRate = 30;

	[Range(1, 16)]
	public int samples = 8;

	public bool supersample = false;

	//[SerializeField, HideInInspector]
	public Shader resolveShader;

	string m_Folder;
	Camera m_Camera;
	Material m_Material;
	Texture2D m_Output;
	int m_FrameCount;

	// socket
	private string host = "localhost";
	private int port = 8990;
	private TcpClient socketConnection; 	
	private Thread clientReceiveThread;

	void OnEnable()
	{
		var dataPath = Application.dataPath;
		dataPath = dataPath.Substring(0, dataPath.Length - 6); // Remove 'Assets'
		var date = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff");
		m_Folder = Path.Combine(dataPath, date);

		try
		{
			Directory.CreateDirectory(m_Folder);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			enabled = false;
		}

		// connect to server
		ConnectToTcpServer(); 

		m_Camera = GetComponent<Camera>();
		m_Material = new Material(resolveShader);
		m_FrameCount = 0;
		Time.captureFramerate = Mathf.Clamp(frameRate, 1, 120);
	}

	void OnDisable()
	{
		Time.captureFramerate = 0;
		m_FrameCount = 0;

		Destroy(m_Material);
		Destroy(m_Output);

		m_Material = null;
		m_Output = null;
	}

	void LateUpdate()
	{
		var cam = m_Camera;
		int ss = supersample ? 2 : 1;
		int w = cam.pixelWidth;
		int h = cam.pixelHeight;
		int sw = w * ss;
		int sh = h * ss;

		var kOutFormat = RenderTextureFormat.ARGB32;
		var kRenderFormat = cam.allowHDR
			? RenderTextureFormat.ARGBHalf
			: RenderTextureFormat.ARGB32;

		var targets = new []
		{
			RenderTexture.GetTemporary(sw, sh, 24, kRenderFormat),
			RenderTexture.GetTemporary(sw, sh, 0, kOutFormat),
			RenderTexture.GetTemporary(sw, sh, 0, kOutFormat),
			RenderTexture.GetTemporary(w, h, 0, kOutFormat)
		};

		var oldActive = RenderTexture.active;
		var oldTarget = cam.targetTexture;

		samples = Mathf.Clamp(samples, 1, 16);
		m_Material.SetFloat("_Samples", samples);

		for (int s = 0; s < samples; s++)
		{
			cam.targetTexture = targets[0];

			if (samples > 1) // Only jitters if we're actually using the temporal filter
				SetProjectionMatrix(cam, s);

			cam.Render();

			if (s == 0)
			{
				Graphics.Blit(targets[0], targets[1]);
			}
			else
			{
				m_Material.SetTexture("_HistoryTex", targets[1]);
				Graphics.Blit(targets[0], targets[2], m_Material, 0);

				// Swap history targets
				var t = targets[1];
				targets[1] = targets[2];
				targets[2] = t;
			}
		}

		var finalTarget = targets[1];
		if (supersample)
		{
			Graphics.Blit(targets[1], targets[3], m_Material, 1);
			finalTarget = targets[3];
		}

		cam.ResetProjectionMatrix();
		cam.targetTexture = oldTarget;

		RenderTexture.active = finalTarget;
		CheckOutput(ref m_Output, w, h);
		m_Output.ReadPixels(new Rect(0, 0, w, h), 0, 0);
		m_Output.Apply();

		RenderTexture.active = oldActive;

		foreach (var target in targets)
			RenderTexture.ReleaseTemporary(target);

//		SaveOutput();
		SendFrames();
		m_FrameCount++;
	}

	void SetProjectionMatrix(Camera cam, int sample)
	{
		const float kJitterScale = 1f;
		var jitter = new Vector2(
			HaltonSeq(sample & 1023, 2),
			HaltonSeq(sample & 1023, 3)
		) * kJitterScale;

		cam.nonJitteredProjectionMatrix = cam.projectionMatrix;

		if (cam.orthographic)
			cam.projectionMatrix = GetOrthoProjectionMatrix(cam, jitter);
		else
			cam.projectionMatrix = GetPerspProjectionMatrix(cam, jitter);

		cam.useJitteredProjectionMatrixForTransparentRendering = false;
	}

	float HaltonSeq(int index, int radix)
	{
		float result = 0f;
		float fraction = 1f / (float)radix;

		while (index > 0)
		{
			result += (float)(index % radix) * fraction;

			index /= radix;
			fraction /= (float)radix;
		}

		return result;
	}

	Matrix4x4 GetPerspProjectionMatrix(Camera cam, Vector2 offset)
	{
		float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView);
		float horizontal = vertical * cam.aspect;
		float near = cam.nearClipPlane;
		float far = cam.farClipPlane;

		offset.x *= horizontal / (0.5f * cam.pixelWidth);
		offset.y *= vertical / (0.5f * cam.pixelHeight);

		float left = (offset.x - horizontal) * near;
		float right = (offset.x + horizontal) * near;
		float top = (offset.y + vertical) * near;
		float bottom = (offset.y - vertical) * near;

		var matrix = new Matrix4x4();

		matrix[0, 0] = (2f * near) / (right - left);
		matrix[0, 1] = 0f;
		matrix[0, 2] = (right + left) / (right - left);
		matrix[0, 3] = 0f;

		matrix[1, 0] = 0f;
		matrix[1, 1] = (2f * near) / (top - bottom);
		matrix[1, 2] = (top + bottom) / (top - bottom);
		matrix[1, 3] = 0f;

		matrix[2, 0] = 0f;
		matrix[2, 1] = 0f;
		matrix[2, 2] = -(far + near) / (far - near);
		matrix[2, 3] = -(2f * far * near) / (far - near);

		matrix[3, 0] = 0f;
		matrix[3, 1] = 0f;
		matrix[3, 2] = -1f;
		matrix[3, 3] = 0f;

		return matrix;
	}

	Matrix4x4 GetOrthoProjectionMatrix(Camera cam, Vector2 offset)
	{
		float vertical = cam.orthographicSize;
		float horizontal = vertical * cam.aspect;

		offset.x *= horizontal / (0.5f * cam.pixelWidth);
		offset.y *= vertical / (0.5f * cam.pixelHeight);

		float left = offset.x - horizontal;
		float right = offset.x + horizontal;
		float top = offset.y + vertical;
		float bottom = offset.y - vertical;

		return Matrix4x4.Ortho(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
	}

	void CheckOutput(ref Texture2D texture, int width, int height)
	{
		if (texture != null && texture.width == width && texture.height == height)
			return;

		Destroy(texture);
		texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
	}

	void SaveOutput()
	{
		try
		{
			var bytes = m_Output.EncodeToPNG();
			var path = Path.Combine(m_Folder, string.Format("{0:D06}.png", m_FrameCount));
			File.WriteAllBytes(path, bytes);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			enabled = false;
		}
	}

	/// Setup socket connection. 	
	private void ConnectToTcpServer () { 		
		try {  

			clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();  		
		} 		
		catch (Exception e) { 			
			Debug.Log("On client connect exception " + e); 		
		} 	
	}  

	/// Runs in background clientReceiveThread; Listens for incomming data. 	
	private void ListenForData() { 		
		try { 			
			socketConnection = new TcpClient(host, port);  			
			// socketConnection = new TcpClient("localhost", 8989);
			Byte[] bytes = new Byte[1024];             
			while (true) { 				
				// Get a stream object for reading 				
				using (NetworkStream stream = socketConnection.GetStream()) { 					
					int length; 					
					// Read incomming stream into byte arrary. 					
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 						
						var incommingData = new byte[length]; 						
						Array.Copy(bytes, 0, incommingData, 0, length); 						
						// Convert byte array to string message. 						
						string serverMessage = Encoding.ASCII.GetString(incommingData); 						
						Debug.Log("server message received as: " + serverMessage); 					
					} 				
				} 			
			}         
		}         
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	}  	

	/// Send message to server using socket connection. 	
	private void SendData() {         
		if (socketConnection == null) {             
			return;         
		}  		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) {                 
				string clientMessage = "This is a message from one of your clients."; 				
				// Convert string message to byte array.                 
				byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage); 				
				// Write byte array to socketConnection stream.                 
				stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
				Debug.Log("Client sent his message - should be received by server");             
			}         
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	} 

	/// Send message to server using socket connection. 	
	private void SendFrames() {         
		if (socketConnection == null) {             
			return;         
		}  		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) {                 
				// Convert string message to byte array. 
//				byte[] frameArray = Encoding.ASCII.GetBytes(m_Output); 	
				byte[] frameArray = m_Output.EncodeToPNG();
				// Write byte array to socketConnection stream.  
//				Debug.Log("size: " + frameArray.Length);
				stream.Write(frameArray, 0, frameArray.Length);     

			}         
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	} 

}

