using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(Texture3D))]
public class Texture3DEditor : Editor
{
    Texture3D           texture;
	public Texture2D	previewX;
	public Texture2D	previewY;
	public Texture2D	previewZ;
    int               	z;
	int					y;
	int					x;

    void OnEnable()
    {
        texture = target as Texture3D;
		previewX = new Texture2D(texture.width, texture.width, TextureFormat.RGBA32, false);
		previewX.filterMode = FilterMode.Point;
		previewY = new Texture2D(texture.width, texture.width, TextureFormat.RGBA32, false);
		previewY.filterMode = FilterMode.Point;
		previewZ = new Texture2D(texture.width, texture.width, TextureFormat.RGBA32, false);
		previewZ.filterMode = FilterMode.Point;
		UpdatePreviewX();
		UpdatePreviewY();
		UpdatePreviewZ();
	}

    public override void OnInspectorGUI()
    {
		DrawDefaultInspector();

		EditorGUI.BeginChangeCheck();
			z = EditorGUILayout.IntSlider(z, -texture.depth / 2, texture.depth / 2 - 1);
		if (EditorGUI.EndChangeCheck())
			UpdatePreviewX();
				EditorGUI.BeginChangeCheck();
			y = EditorGUILayout.IntSlider(y, -texture.depth / 2, texture.depth / 2 - 1);
		if (EditorGUI.EndChangeCheck())
			UpdatePreviewY();
				EditorGUI.BeginChangeCheck();
			x = EditorGUILayout.IntSlider(x, -texture.depth / 2, texture.depth / 2 - 1);
		if (EditorGUI.EndChangeCheck())
			UpdatePreviewZ();

		Rect previewRect = EditorGUILayout.GetControlRect(false, texture.width);
		if (previewX != null)
			EditorGUI.DrawPreviewTexture(previewRect, previewX, null, ScaleMode.ScaleToFit);
		previewRect = EditorGUILayout.GetControlRect(false, texture.width);
		if (previewY != null)
			EditorGUI.DrawPreviewTexture(previewRect, previewY, null, ScaleMode.ScaleToFit);
		previewRect = EditorGUILayout.GetControlRect(false, texture.width);
		if (previewZ != null)
			EditorGUI.DrawPreviewTexture(previewRect, previewZ, null, ScaleMode.ScaleToFit);

    }

	void UpdatePreviewX()
	{
		Color[] pixels = texture.GetPixels();

		for (int x = 0; x < texture.width; x++)
			for (int y = 0; y < texture.height; y++)
			{
				Color c = pixels[x + y * texture.width + (z + texture.width / 2) * texture.width * texture.height];
				if (c.r + c.b + c.b > 2.5f)
					previewX.SetPixel(x, y, new Color(c.a, c.a, c.a, 1));
				else
					previewX.SetPixel(x, y, c);
			}
		previewX.Apply();
	}

	void UpdatePreviewY()
	{
		Color[] pixels = texture.GetPixels();

		for (int x = 0; x < texture.width; x++)
			for (int z = 0; z < texture.height; z++)
			{
				Color c = pixels[x + (y + texture.width / 2) * texture.width + z * texture.width * texture.height];
				if (c.r + c.b + c.b > 2.5f)
					previewY.SetPixel(x, z, new Color(c.a, c.a, c.a, 1));
				else
					previewY.SetPixel(x, z, c);
			}
		previewY.Apply();
	}

	void UpdatePreviewZ()
	{
		Color[] pixels = texture.GetPixels();

		for (int y = 0; y < texture.width; y++)
			for (int z = 0; z < texture.height; z++)
			{
				Color c = pixels[(x + texture.height / 2) + y * texture.width + z * texture.width * texture.height];
				if (c.r + c.b + c.b > 2.5f)
					previewZ.SetPixel(z, y, new Color(c.a, c.a, c.a, 1));
				else
					previewZ.SetPixel(z, y, c);
			}
		previewZ.Apply();
	}

    void OnDisable()
    {

    }
}
