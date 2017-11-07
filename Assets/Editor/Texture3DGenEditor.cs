using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(Texture3DNoiseGenerator))]
public class Texture3DGenEditor : Editor {

	public Texture2D				preview;
	public int						oldTextureSize;
	public Texture3DNoiseGenerator	noiseGen;

	public void OnEnable()
	{
		noiseGen = target as Texture3DNoiseGenerator;
		oldTextureSize = noiseGen.textureSize;
		preview = new Texture2D(noiseGen.textureSize, noiseGen.textureSize, TextureFormat.RGBA32, false);
		preview.filterMode = FilterMode.Point;
		UpdatePreview();
	}

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		{
			DrawDefaultInspector();
		}
		if (EditorGUI.EndChangeCheck())
			UpdatePreview();

		Rect previewRect = EditorGUILayout.GetControlRect(false, noiseGen.textureSize);
		EditorGUI.DrawPreviewTexture(previewRect, preview, null, ScaleMode.ScaleToFit);

		EditorGUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("Export"))
				noiseGen.Export();
			if (GUILayout.Button("Cancel"))
				noiseGen.Stop();
		}
		EditorGUILayout.EndHorizontal();
	}

	void UpdatePreview()
	{
		if (oldTextureSize != noiseGen.textureSize)
		{
			preview = new Texture2D(noiseGen.textureSize, noiseGen.textureSize, TextureFormat.RGBA32, false);
			preview.filterMode = FilterMode.Point;
		}
		
		for (int x = 0; x < noiseGen.textureSize; x++)
			for (int y = 0; y < noiseGen.textureSize; y++)
			{
				Color c = Color.black;
				for (int i = 0 ; i < noiseGen.channels; i++)
				{
					float f = noiseGen.GetPoint(x - noiseGen.textureSize * i, y - noiseGen.textureSize * i, noiseGen.textureSize / 2, 0);

					f += noiseGen.adjust;

					if (f < noiseGen.cutoff)
						c[i] = 0;
					else
						c[i] = f;
				}
				preview.SetPixel(x, y, Color.white * c);
			}
		preview.Apply();
	}

}
