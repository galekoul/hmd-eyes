﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OperatorMonitor : MonoBehaviour {

	private static PupilGazeTracker pupilTracker;
	private PupilSettings pupilSettings;

	private static Texture2D _texture;
	private int MainCameraTargetDisplay = 0;

	public static Matrix4x4 _offsetMatrix;
	public Vector3 _v3 = Vector3.one;
	public Vector3 _s3 = Vector3.one;
	public Vector3 _r3 = Vector3.one;

	public float[] confidences = new float[]{ 0f, 0f };

	static OperatorMonitor _Instance;
	public static OperatorMonitor Instance
	{
		get{
			return _Instance;
		}
	}

	public static void Instantiate(){

		_texture = new Texture2D (1, 6);
		_texture.SetPixel (0, 0, new Color (1, 1, 1, .6f));
		_texture.SetPixel (0, 1, new Color (1, 1, 1, .5f));
		_texture.SetPixel (0, 2, new Color (1, 1, 1, .4f));
		_texture.SetPixel (0, 3, new Color (1, 1, 1, .3f));
		_texture.SetPixel (0, 4, new Color (1, 1, 1, .2f));
		_texture.SetPixel (0, 5, new Color (1, 1, 1, .1f));
		_texture.Apply ();

		GameObject _camGO = new GameObject ();
		_camGO.name = "Operator Camera";
		OperatorMonitor _opscript = _camGO.AddComponent<OperatorMonitor> ();
		Camera _cam = _camGO.GetComponent<Camera> ();
		pupilTracker = PupilGazeTracker.Instance;

		Operator.properties.Properties = pupilTracker.OperatorMonitorProperties;

		Operator.properties.Properties [0].OperatorCamera = _cam;
		_cam.stereoTargetEye = StereoTargetEyeMask.None;
//		_cam.backgroundColor = Color.gray;
		_cam.transform.parent = PupilSettings.Instance.currentCamera.transform;
		_cam.transform.localPosition = Vector3.zero;
		_cam.transform.localEulerAngles = Vector3.zero;
		_cam.fieldOfView = PupilSettings.Instance.currentCamera.fieldOfView;
		_cam.clearFlags = CameraClearFlags.Depth;

		_opscript.MainCameraTargetDisplay = PupilSettings.Instance.currentCamera.targetDisplay;
		//		PupilSettings.Instance.currentCamera.targetDisplay = 1;


		Operator.properties.Properties [0].confidenceList.Capacity = Operator.properties.Properties [0].graphLength + 1;
		Operator.properties.Properties [1].confidenceList.Capacity = Operator.properties.Properties [1].graphLength + 1;

		_offsetMatrix = new Matrix4x4 ();

		PupilTools.SubscribeTo ("pupil.");

		PupilTools.StartFramePublishing ();
	}
//	void OnDestroy(){
//		pupilTracker.StopFramePublishing ();
//		pupilTracker.isOperatorMonitor = false;
	//		PupilSettings.Instance.currentCamera.targetDisplay = MainCameraTargetDisplay;
//	}

	void Awake(){
		pupilTracker = PupilGazeTracker.Instance;
		pupilSettings = pupilTracker.Settings;
		PupilSettings.Instance.currentCamera.SetReplacementShader (CameraShader, null);	
		_Instance = this;
	}

	public void ExitOperatorMonitor()
	{
		PupilTools.UnSubscribeFrom ("pupil.");

		if (!PupilSettings.Instance.debugView.active && !pupilTracker.isOperatorMonitor)
		{	
			PupilTools.StopFramePublishing ();
		}
		pupilTracker.isOperatorMonitor = false;
		PupilSettings.Instance.currentCamera.targetDisplay = MainCameraTargetDisplay;
		Destroy (gameObject);
	}

//	bool requestUpdate = false;
//	bool isMouseDown = false;
	public Shader CameraShader;
	void OnGUI()
	{
#if !UNITY_WSA
		string str;

//		print ("confidence 0 in op mon : " + Pupil.values.Confidences [0]);
		Operator.properties.Properties [0].confidence = PupilTools.Confidence(PupilData.leftEyeID);
		Operator.properties.Properties [1].confidence = PupilTools.Confidence (PupilData.rightEyeID);

//		print (Pupil.values.Confidences [0]);

		GUI.color = new Color (1, 1, 1, .5f);

		float imageHeight = (Screen.width / 2) / 1.333f; //for 4:3 ratio
		float imageVerticalPosition = (Screen.height-imageHeight)/2;	

		GUI.DrawTexture (new Rect (0, imageVerticalPosition, Screen.width / 2, imageHeight), pupilSettings.framePublishing.eye0Image);
		GUI.DrawTexture (new Rect (Screen.width / 2, imageVerticalPosition, Screen.width / 2, imageHeight), pupilSettings.framePublishing.eye1Image);

		Operator.properties.Properties [0].OperatorCamera.Render ();


		//Construct the Text box string for data display on the Operator Monitor view
		str = "Gaze Point : " + " ( X: " + PupilData._3D.GazePosition.x + " Y: " + PupilData._3D.GazePosition.y + " Z: " + PupilData._3D.GazePosition.z + " ) ";
		str += "\nEyeball 0 Center : " + " ( X: " + PupilData._3D.RightEyeCenter.x + " Y: " + PupilData._3D.RightEyeCenter.y + " Z: " + PupilData._3D.RightEyeCenter.z + " ) ";
		str += "\nEyeball 1 Center : " + " ( X: " + PupilData._3D.LeftEyeCenter.x + " Y: " + PupilData._3D.LeftEyeCenter.y + " Z: " + PupilData._3D.LeftEyeCenter.z + " ) ";
		str += "\nPupil Diameter : " + PupilData.Diameter ();


		//Use the predefined style for the TextArea
		GUIStyle _s = pupilTracker.Styles.Find (x => x.name == "OpMon_textArea");
		GUI.TextArea (new	 Rect (0, 0, Screen.width, 200), str, _s);

		//This is the call to draw both Confidence Graphs for each eyes
		DrawGraph (Operator.properties.Properties[0]);
		DrawGraph (Operator.properties.Properties[1]);
#endif
	}
		
	#region operator_monitor.functions

//	private int similarIndex = 0;
//	private float lastConfidence = 0f;
	public void DrawGraph( Operator.properties _props ){

		//Enabling the graph data to update with a certain delay, definec under the static Operator.properties
		if (TimeSpan.FromTicks(DateTime.Now.Ticks - _props.graphTime).TotalSeconds > (_props.refreshDelay/100)) {
			_props.update = true;
			_props.graphTime = DateTime.Now.Ticks;
		}

		//if update is allowed add the current confidence level for the current eye in the relevant confidence level list;
		if (_props.update) {
			_props.update = false;
			_props.confidenceList.Insert (0, _props.confidence);

			if (_props.confidenceList.Count > _props.graphLength)//limit the confidence level list to the graph length variable. If exceeded cut the last one.
				_props.confidenceList.RemoveAt (_props.confidenceList.Count - 1);
		}
	
		//if the current confidence level list reached the size required start drawing the graph. (this might be subject to change)
		if (_props.confidenceList.Count >= _props.graphLength) {

			//TODO: clean this up!
			pupilTracker.Styles [2].normal.background = _texture;
			Color _c = new Color (1,1,1,1);
			GUI.matrix = Matrix4x4.TRS (new Vector3((Screen.width/2)*_props.positionOffset.x,(Screen.height/2)*_props.positionOffset.y,1), Quaternion.Euler (_props.rotationOffset), new Vector3(Screen.width*_props.scaleOffset.x,Screen.height*_props.scaleOffset.y,1));
			for (int i = 0; i < _props.graphLength; i++) {
				_c.a = Mathf.InverseLerp (0, (_props.graphLength / 2), (_props.graphLength / 2) - Mathf.Abs ((i - (_props.graphLength / 2))));
				GUI.color = _c;
				GUI.Box (new Rect ((i * _props.gapSize), 0, _props.graphScale.x, _props.confidenceList [i] * _props.graphScale.y),"", pupilTracker.Styles [2]);
			}
		}

	}
	#endregion
}
