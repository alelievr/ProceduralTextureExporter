using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Texture3DSettings : ScriptableObject
{
	public int		textureSize = 256;
	public int		channels = 1;
	
	public int		seed = 42;
	public float	scale = 2;
	public int		octaves = 2;
	public float	frequency = 2;
	public float	lacunarity = 1;
	public float	persistence = 1;
	
	[Range(0, 1)]
	public float	cutoff = .3f;
	public float	multiplicator = 2f;
	public float	adjust = 0f;
	public Vector3	offset = Vector3.zero;
	[Range(0.01f, 2f)]
	public float	islandScale = 1f;
	[Range(0, 2f)]
	public float	islandCoreSize = .5f;
	[Range(1, 10)]
	public float	islandPow = 1f;
	public bool		viewMask = false;
    public AnimationCurve remapNoise;


    public List< DensityPoint >	densityPoints = new List< DensityPoint >();

	[MenuItem("Assets/Create/New Texture3DSettings")]
	public static void CreateTextureSettings()
	{
		var ts = Texture3DSettings.CreateInstance< Texture3DSettings >();

		string path = "Assets/Textures/new texture3D.asset";

		path = AssetDatabase.GenerateUniqueAssetPath(path);

		Debug.Log("Path: " + path);
		ProjectWindowUtil.CreateAsset(ts, path);
	}
	
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

		float dist = 1e20f;

		foreach (var densityPoint in densityPoints)
		{
			float t = Vector3.Magnitude((new Vector3(p, q, r) - offset) - densityPoint.position) * densityPoint.radius;
			dist = Mathf.Min(dist, t);
		}

		float mid = islandScale / (dist + islandCoreSize);
		mid = Mathf.Clamp01(Mathf.Pow(mid, islandPow));

		ret = (perlin + adjust) * mid * multiplicator;

        ret = remapNoise.Evaluate(ret);


		if (viewMask)
			ret = mid;

		return ret;
	}
	
	public void GenNoisePart(byte[] fileBytes, float part, int numThreads)
	{
		int fromX = (int)(textureSize / (numThreads) * part);
		int toX = (int)(textureSize / (numThreads) * (part + 1));

		Debug.Log("Thread started from " + fromX + " to " + toX + ", part: " + part);
		
		try {
		
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
						
						if (f < cutoff)
							fileBytes[ci] = 0;
						else
							fileBytes[ci] = System.Convert.ToByte(Mathf.Clamp(f * 255f, 0, 255));
					}
		}
		
		} catch (System.Exception e) {
			Debug.LogError(e);
		}

		Debug.Log("Thread finished from " + fromX + " to " + toX + ", part: " + part);
	}

}
