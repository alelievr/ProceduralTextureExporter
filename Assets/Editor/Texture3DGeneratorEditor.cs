using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Texture3DNoiseGenerator))]
public class Texture3DGeneratorEditor : Editor
{
	public Texture3DNoiseGenerator	noiseGenerator;
	public Texture2D				preview;

	public float					zPosition = 0;

	[System.NonSerialized]
	int								oldTextureSize = -1;

	void OnEnable()
	{
		noiseGenerator = target as Texture3DNoiseGenerator;

		UpdatePreview();
	}

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		{
			DrawDefaultInspector();
			zPosition = EditorGUILayout.Slider(zPosition, -noiseGenerator.settings.textureSize / 2, noiseGenerator.settings.textureSize / 2);
		}
		if (EditorGUI.EndChangeCheck())
			UpdatePreview();

		Rect previewRect = EditorGUILayout.GetControlRect(false, noiseGenerator.settings.textureSize);
		if (preview != null)
			EditorGUI.DrawPreviewTexture(previewRect, preview, null, ScaleMode.ScaleToFit);
		
		EditorGUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("Export"))
				noiseGenerator.Export();
			if (GUILayout.Button("Cancel"))
				noiseGenerator.Stop();
			if (GUILayout.Button("Update"))
				UpdatePreview();
		}
		EditorGUILayout.EndHorizontal();
	}
	
	void UpdatePreview()
	{
        if (noiseGenerator.settings == null)
            return ;

		if (oldTextureSize != noiseGenerator.settings.textureSize)
		{
			preview = new Texture2D(noiseGenerator.settings.textureSize, noiseGenerator.settings.textureSize, TextureFormat.RGBA32, false);
			preview.filterMode = FilterMode.Point;
			oldTextureSize = noiseGenerator.settings.textureSize;
		}
		
		for (int x = 0; x < noiseGenerator.settings.textureSize; x++)
			for (int y = 0; y < noiseGenerator.settings.textureSize; y++)
			{
				Color c = Color.black;
				for (int i = 0 ; i < noiseGenerator.settings.channels; i++)
				{
					float f = noiseGenerator.settings.GetPoint(x - noiseGenerator.settings.textureSize * i, y - noiseGenerator.settings.textureSize * i, noiseGenerator.settings.textureSize / 2 + zPosition, 0);

					if (f < noiseGenerator.settings.cutoff)
						c[i] = 0;
					else
						c[i] = f;
				}
				preview.SetPixel(x, y, Color.white * c);
			}
		preview.Apply();
	}
}
