using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using UnityEngine.UI;
using UnityEditor;

[System.Serializable]
public struct DensityPoint
{
	public Vector3	position;
	public float	radius;
}

[ExecuteInEditMode]
public class Texture3DNoiseGenerator : MonoBehaviour
{
	public Texture3DSettings	settings;
	public int					numThreads;

	float	progress = 0;

	[SerializeField, HideInInspector]
	bool			running = false;

	public void Export()
	{
		if (running)
		{
			Debug.Log("another export is alredy in progress");
			return ;
		}
		byte[]	fileBytes = new byte[settings.textureSize * settings.textureSize * settings.textureSize * settings.channels];

		ExportNoise(fileBytes);
	}

	public void Stop()
	{
		running = false;
	}

	void OnDisable()
	{
		running = false;
	}

	void ExportNoise(byte[] fileBytes)
	{
		running = true;
		Thread[] threads = new Thread[numThreads];
		for (int i = 0; i < numThreads; i++)
		{
			threads[i] = new Thread(() => {
				settings.GenNoisePart(fileBytes, i, numThreads);
			});
			threads[i].Start();
			Thread.Sleep(20);
		}

		for (int i = 0; i < numThreads; i++)
			threads[i].Join();
		
		// File.WriteAllBytes("/goinfre/" + name + "-" + settings.textureSize + "s" + settings.channels +"c" + ".3Draw", fileBytes);
		
		string path = "Assets/Textures/texture-" + settings.textureSize + "s" + settings.channels + "c.asset";

		path = AssetDatabase.GenerateUniqueAssetPath(path);

		ExportTexture(fileBytes, path);

		Debug.Log("Done !");
		running = false;
	}

	void ExportTexture(byte[] fileBytes, string path)
	{
		Color[] colors = new Color[settings.textureSize * settings.textureSize * settings.textureSize + 10];

		TextureFormat texFormat = (settings.channels == 1) ? TextureFormat.Alpha8 : ((settings.channels == 2) ? TextureFormat.RG16 : (settings.channels == 3) ? TextureFormat.RGB24 : TextureFormat.RGBA32);
		Texture3D	tex = new Texture3D(settings.textureSize, settings.textureSize, settings.textureSize, texFormat, true);
		tex.filterMode = FilterMode.Trilinear;

		for (int x = 0; x < settings.textureSize; x++)
			for (int y = 0; y < settings.textureSize; y++)
				for (int z = -settings.textureSize / 2; z < settings.textureSize / 2; z++)
				{
					int cx = x * settings.textureSize * settings.textureSize;
					int cy = y * settings.textureSize;
					int cz = z + settings.textureSize / 2;
					int ci = (cx + cy + cz) * settings.channels;

					Color c = new Color();

					if (settings.channels == 1)
						c[3] = c[2] = c[1] = c[0] = (float)(fileBytes[ci]) / 255f;
					else
						for (int i = 0; i < settings.channels; i++)
							c[i] = (float)(fileBytes[ci + i]) / 255f;

					colors[cx + cy + cz] = c;
				}
			
		tex.SetPixels(colors);
		tex.Apply();

		AssetDatabase.CreateAsset(tex, path);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.G))
			Export();
		if (Input.GetKeyDown(KeyCode.C))
			Stop();
	}
}
