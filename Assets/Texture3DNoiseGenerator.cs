using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using UnityEngine.UI;
using UnityEditor;

[ExecuteInEditMode]
public class Texture3DNoiseGenerator : MonoBehaviour {

	public int		numThreads;
	[Space]
	public int		textureSize = 256;
	public int		channels = 1;
	public string	noiseName;
	[Space]
	public int		seed = 42;
	public float	scale = 2;
	public int		octaves = 2;
	public float	frequency = 2;
	public float	lacunarity = 1;
	public float	persistence = 1;

	[Space]
	[Range(0, 1)]
	public float	cutoff = .3f;
	public float	adjust = 0f;
	public Vector3	offset = Vector3.zero;
	[Range(0.01f, 2f)]
	public float	islandScale = 1f;
	[Range(0, 2f)]
	public float	islandCoreSize = .5f;
	[Range(1, 10)]
	public float	islandPow = 1f;
	public bool		viewMask = false;

	[Space]

	[HideInInspector]
	public bool		arrayExport = false;

	float	progress = 0;

	[SerializeField, HideInInspector]
	bool			running = false;

	static float pi2 = 3.14159265f * 2.0f;

	struct Range
	{
		public Vector3	loop0;
		public Vector3	loop1;

		public Vector3	map0;
		public Vector3	map1;
	}

	static Range	ranges;

	public float GetPoint(float x, float y, float z, int seed)
	{
		float p = (x - textureSize / 2) / scale + offset.x;
		float q = (y - textureSize / 2) / scale + offset.y;
		float r = (z - textureSize / 2) / scale + offset.z;
		/*p = p * (ranges.map1.x - ranges.map0.x) / (ranges.loop1.x - ranges.loop0.x);
		q = q * (ranges.map1.y - ranges.map0.y) / (ranges.loop1.y - ranges.loop0.y);
		r = r * (ranges.map1.z - ranges.map0.z) / (ranges.loop1.z - ranges.loop0.z);
		float dx = ranges.loop1.x - ranges.loop0.x;
		float dy = ranges.loop1.y - ranges.loop0.y;
		float dz = ranges.loop1.z - ranges.loop0.z;
		float nx = ranges.loop0.x + Mathf.Cos(p * pi2) * dx / pi2;
		float ny = ranges.loop0.x + Mathf.Sin(p * pi2) * dx / pi2;
		float nz = ranges.loop0.y + Mathf.Cos(q * pi2) * dy / pi2;
		float nw = ranges.loop0.y + Mathf.Sin(q * pi2) * dy / pi2;
		float nu = ranges.loop0.z + Mathf.Cos(r * pi2) * dz / pi2;
		float nv = ranges.loop0.z + Mathf.Sin(r * pi2) * dz / pi2;
		return Simplex6D.GetValue(nx, ny, nz, nw, nu, nv, 42);*/

		// return Simplex6D.GetValue(x, y, z, 45, 10, -23, 42);
		float perlin = PerlinNoise3D.GenerateNoise(p, q, r, octaves, frequency, lacunarity, persistence, seed + this.seed);
		float ret = 0;

		float mid = islandScale / (Vector3.Magnitude(new Vector3(p, q, r)) + islandCoreSize);
		mid = Mathf.Clamp01(Mathf.Pow(mid, islandPow));
		if (mid < .05f)
			mid = 0;

		ret = perlin * mid;

		if (viewMask)
			ret = mid;

		return ret;
	}
	
	public void Export()
	{
		if (running)
		{
			Debug.Log("another export is alredy in progress");
			return ;
		}
		byte[]	fileBytes = new byte[textureSize * textureSize * textureSize * channels];

		ranges = new Range();

		ranges.loop0 = Vector3.one * -1;
		ranges.loop1 = Vector3.one * 1;
		ranges.map0 = Vector3.one * -128;
		ranges.map1 = Vector3.one * 128;

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

	void GenNoisePart(byte[] fileBytes, float part)
	{
		int fromX = (int)(textureSize / (numThreads) * part);
		int toX = (int)(textureSize / (numThreads) * (part + 1));

		Debug.Log("Thread started from " + fromX + " to " + toX + ", part: " + part);
		for (int x = fromX; x < toX; x++)
		{
			for (int y = 0; y < textureSize; y++)
				for (int z = 0; z < textureSize; z++)
					for (int i = 0; i < channels; i++)
					{
						int cx = x * textureSize * textureSize;
						int cy = y * textureSize;
						int cz = z;
						int ci = (cx + cy + cz) * channels + i;
						float f = GetPoint(x, y, z, i * 183);
						
						f += adjust;

						if (f < cutoff)
							fileBytes[ci] = 0;
						else
							fileBytes[ci] = System.Convert.ToByte(f * 255f);
					}
			progress = ((float)x / (float)textureSize) * 100f;
			Debug.Log("progress: " + progress + "%");
		}
		Debug.Log("Thread finished from " + fromX + " to " + toX + ", part: " + part);
	}

	void ExportNoise(byte[] fileBytes)
	{
		running = true;
		Thread[] threads = new Thread[numThreads];
		for (int i = 0; i < numThreads; i++)
		{
			threads[i] = new Thread(() => {
				GenNoisePart(fileBytes, i);
			});
			threads[i].Start();
			Thread.Sleep(20);
		}

		for (int i = 0; i < numThreads; i++)
			threads[i].Join();
		
		File.WriteAllBytes("/goinfre/" + name + "-" + textureSize + "s" + channels +"c" + ".3Draw", fileBytes);
		
		string path = "Assets/texture-" + textureSize + "s" + channels + "c.asset";

		path = AssetDatabase.GenerateUniqueAssetPath(path);

		if (arrayExport)
			ExportTextureAsArray(fileBytes, path);
		else
			ExportTexture(fileBytes, path);

		Debug.Log("Done !");
		running = false;
	}

	void ExportTexture(byte[] fileBytes, string path)
	{
		Color[] colors = new Color[textureSize * textureSize * textureSize + 10];

		TextureFormat texFormat = (channels == 1) ? TextureFormat.Alpha8 : ((channels == 2) ? TextureFormat.RG16 : (channels == 3) ? TextureFormat.RGB24 : TextureFormat.RGBA32);
		Texture3D	tex = new Texture3D(textureSize, textureSize, textureSize, texFormat, true);
		tex.filterMode = FilterMode.Trilinear;

		for (int x = 0; x < textureSize; x++)
			for (int y = 0; y < textureSize; y++)
				for (int z = -textureSize / 2; z < textureSize / 2; z++)
				{
					int cx = x * textureSize * textureSize;
					int cy = y * textureSize;
					int cz = z + textureSize / 2;
					int ci = (cx + cy + cz) * channels;

					Color c = new Color();

					if (channels == 1)
						c[3] = c[2] = c[1] = c[0] = (float)(fileBytes[ci]) / 255f;
					else
						for (int i = 0; i < channels; i++)
							c[i] = (float)(fileBytes[ci + i]) / 255f;

					colors[cx + cy + cz] = c;
				}
			
		tex.SetPixels(colors);
		tex.Apply();

		AssetDatabase.CreateAsset(tex, path);
	}

	void ExportTextureAsArray(byte[] fileBytes, string path)
	{
		TextureFormat texFormat = (channels == 1) ? TextureFormat.Alpha8 : ((channels == 2) ? TextureFormat.RG16 : (channels == 3) ? TextureFormat.RGB24 : TextureFormat.RGBA32);
		Texture2DArray	arr = new Texture2DArray(textureSize, textureSize, textureSize, texFormat, false);
		
		for (int x = 0; x < textureSize; x++)
		{
			Color[] colors = new Color[textureSize * textureSize];
			for (int y = 0; y < textureSize; y++)
				for (int z = 0; z < textureSize; z++)
				{
					int cy = y * textureSize;
					int cz = z;
					int ci = (cy + cz) * channels;

					Color c = new Color();

					if (channels == 1)
						c[3] = c[2] = c[1] = c[0] = (float)(fileBytes[ci]) / 255f;
					else
						for (int i = 0; i < channels; i++)
							c[i] = (float)(fileBytes[ci + i]) / 255f;

					colors[cy + cz] = c;
				}
			arr.SetPixels(colors, x, 0);
		}
		
		arr.Apply();

		AssetDatabase.CreateAsset(arr, path);
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.G))
			Export();
		if (Input.GetKeyDown(KeyCode.C))
			Stop();
	}
}
