using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(Texture3DSettings))]
public class Texture3DSettingsEditor : Editor {

	public Texture2D			preview;
	public Texture3DSettings	settings;
	
	[System.NonSerialized]
	int							oldTextureSize = -1;

	Vector3						normalTest;
	
	float						changedTime;
	float						delayedChangeTimeout = .3f;
	bool						changed;
	
	float						saveTimeout = 2f;
	float						lastSaveTime;
	static bool					f = false;
	static int					inst;

	public void OnEnable()
	{
		settings = target as Texture3DSettings;
		oldTextureSize = settings.textureSize;
		preview = new Texture2D(settings.textureSize, settings.textureSize, TextureFormat.RGBA32, false);
		preview.filterMode = FilterMode.Point;
		UpdatePreview();
	
		if (f == false)
			inst = GetHashCode();
		else
			return ;
		f = true;
		SceneView.onSceneGUIDelegate += OnSceneGUI;
	}

	void OnSceneGUI()
	{
	}

	void OnDisable()
	{
		f = false;
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		{
			DrawDefaultInspector();
		}
		if (EditorGUI.EndChangeCheck())
			UpdatePreview();

		Rect previewRect = EditorGUILayout.GetControlRect(false, settings.textureSize);
		EditorGUI.DrawPreviewTexture(previewRect, preview, null, ScaleMode.ScaleToFit);
	}

	void OnSceneGUI(SceneView sv)
	{
		var e = Event.current;

		if (settings.densityPoints.Count <= 0)
			settings.densityPoints.Add(new DensityPoint());

		DensityPoint last = default(DensityPoint);
		
		for (int i = 0; i < settings.densityPoints.Count; i++)
		{
			var dp = settings.densityPoints[i];
			dp.position = Handles.PositionHandle(dp.position, Quaternion.identity);
			Handles.Label(dp.position, dp.position.ToString("F2"));

			settings.densityPoints[i] = dp;

			last = dp;
		}
		if (GUI.changed)
		{
			changed = true;
			changedTime = Time.time;
		}

		//draw normal preview:
		Vector3 normalDir = settings.GetNormal(normalTest.x, normalTest.y, normalTest.z);
		if (inst == GetHashCode())
		Handles.ArrowHandleCap(0, normalTest, Quaternion.LookRotation(normalDir), .1f, e.type);
		normalTest = Handles.FreeMoveHandle(normalTest, Quaternion.identity, .02f, Vector3.zero, Handles.DotHandleCap);

		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.N)
		{
			settings.densityPoints.Add(last);
			changed = true;
			changedTime = Time.time;
			e.Use();
		}

		if (changed && Time.time - changedTime > delayedChangeTimeout)
		{
			Undo.RecordObject(this, "changed density point");
			UpdatePreview();
			Repaint();
			changed = false;
		}

		if (Time.time - lastSaveTime > saveTimeout)
		{
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			lastSaveTime = Time.time;
		}
	}

	void UpdatePreview()
	{
		if (oldTextureSize != settings.textureSize)
		{
			preview = new Texture2D(settings.textureSize, settings.textureSize, TextureFormat.RGBA32, false);
			preview.filterMode = FilterMode.Point;
		}

		for (int x = 0; x < settings.textureSize; x++)
			for (int y = 0; y < settings.textureSize; y++)
			{
				Color c = Color.black;
				for (int i = 0 ; i < settings.channels; i++)
				{
					float f = settings.GetPoint(x - settings.textureSize * i, y - settings.textureSize * i, settings.textureSize / 2, settings.seed, settings.remapNoise);

					if (f < settings.cutoff)
						c[i] = 0;
					else
						c[i] = f;
				}
				preview.SetPixel(x, y, c);
			}
		preview.Apply();
	}

}
