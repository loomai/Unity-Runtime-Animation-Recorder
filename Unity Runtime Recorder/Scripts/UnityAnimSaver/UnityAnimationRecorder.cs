﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class UnityAnimationRecorder : MonoBehaviour {

	// save file path
	public string savePath;
	public string fileName;

	// use it when save multiple files
	int fileIndex = 0;

	public KeyCode startRecordKey = KeyCode.Q;
	public KeyCode stopRecordKey = KeyCode.W;

	// options
	public bool showLogGUI = false;
	string logMessage = "";

	public bool recordLimitedFrames = false;
	public int recordFrames = 1000;
	int frameIndex = 0;

	public bool changeTimeScale = false;
	public float timeScaleOnStart = 0.0f;
	public float timeScaleOnRecord = 1.0f;

	public bool recordBlendShape = true;

    public List<string> transformWhiteList = null;

    Transform[] recordObjs;
	SkinnedMeshRenderer[] blendShapeObjs;
	List<UnityObjectAnimation> objRecorders;
	List<UnityBlendShapeAnimation> blendShapeRecorders;

	bool isStart = false;
	float nowTime = 0.0f;

	// Use this for initialization
	void Start () {
		SetupRecorders ();

	}

	void SetupRecorders () {
		recordObjs = gameObject.GetComponentsInChildren<Transform> ();
        objRecorders = new List<UnityObjectAnimation>();
		blendShapeRecorders = new List<UnityBlendShapeAnimation> ();

		frameIndex = 0;
		nowTime = 0.0f;

		for (int i = 0; i < recordObjs.Length; i++) {

            // check if a whitelist is set and if so filter objects
            if (transformWhiteList == null || transformWhiteList.Contains(recordObjs[i].name))
            {
                string path = AnimationRecorderHelper.GetTransformPathName(transform, recordObjs[i]);
                objRecorders.Add(new UnityObjectAnimation(path, recordObjs[i]));
            }

			// check if theres blendShape
			if (recordBlendShape) {
				if (recordObjs [i].GetComponent<SkinnedMeshRenderer> ()) {
					SkinnedMeshRenderer tempSkinMeshRenderer = recordObjs [i].GetComponent<SkinnedMeshRenderer> ();

					// there is blendShape exist
					if (tempSkinMeshRenderer.sharedMesh.blendShapeCount > 0) {
                        string path = AnimationRecorderHelper.GetTransformPathName(transform, recordObjs[i]);
                        blendShapeRecorders.Add (new UnityBlendShapeAnimation (path, tempSkinMeshRenderer));
					}
				}
			}
		}

		if (changeTimeScale)
			Time.timeScale = timeScaleOnStart;
	}
	
	// Update is called once per frame
	void LateUpdate () {
	
		if (Input.GetKeyDown (startRecordKey)) {
			StartRecording ();
		}

		if (Input.GetKeyDown (stopRecordKey)) {
			StopRecording ();
		}

		if (isStart) {
			nowTime += Time.deltaTime;

            foreach (var recorder in objRecorders)
            {
                recorder.AddFrame(nowTime);
            }

            foreach (var recorder in blendShapeRecorders)
            {
                recorder.AddFrame(nowTime);
            }
		}

	}

	public void StartRecording () {
		CustomDebug ("Start Recorder");
		isStart = true;
		Time.timeScale = timeScaleOnRecord;
	}


	public void StopRecording () {
		CustomDebug ("End Record, generating .anim file");
		isStart = false;

		ExportAnimationClip ();
		ResetRecorder ();
	}

	public void ResetRecorder () {
		SetupRecorders ();
	}


	void FixedUpdate () {

		if (isStart) {
			if (recordLimitedFrames) {
				if (frameIndex < recordFrames) {

                    foreach (var recorder in objRecorders)
                    {
                        recorder.AddFrame(nowTime);
                    }

                    ++frameIndex;
				}
				else {
					isStart = false;
					ExportAnimationClip ();
					CustomDebug ("Recording Finish, generating .anim file");
				}
			}

		}
	}

	void OnGUI () {
		if (showLogGUI)
			GUILayout.Label (logMessage);
	}

	void ExportAnimationClip () {

		string exportFilePath = savePath + fileName;

		// if record multiple files when run
		if (fileIndex != 0)
			exportFilePath += "-" + fileIndex + ".anim";
		else
			exportFilePath += ".anim";


		AnimationClip clip = new AnimationClip ();
		clip.name = fileName;

		for (int i = 0; i < objRecorders.Count; i++) {
			UnityCurveContainer[] curves = objRecorders [i].curves;

			for (int x = 0; x < curves.Length; x++) {
				clip.SetCurve (objRecorders [i].pathName, typeof(Transform), curves [x].propertyName, curves [x].animCurve);
			}
		}

		if (recordBlendShape) {
			for (int i = 0; i < blendShapeRecorders.Count; i++) {

				UnityCurveContainer[] curves = blendShapeRecorders [i].curves;

				for (int x = 0; x < curves.Length; x++) {
					clip.SetCurve (blendShapeRecorders [i].pathName, typeof(SkinnedMeshRenderer), curves [x].propertyName, curves [x].animCurve);
				}
				
			}
		}

		clip.EnsureQuaternionContinuity ();
		AssetDatabase.CreateAsset ( clip, exportFilePath );

		CustomDebug (".anim file generated to " + exportFilePath);
		fileIndex++;
	}

	void CustomDebug ( string message ) {
		if (showLogGUI)
			logMessage = message;
		else
			Debug.Log (message);
	}
}
#endif
