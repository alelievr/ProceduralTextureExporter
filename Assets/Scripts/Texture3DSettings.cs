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

	public bool		computeNormals = false;
	
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
	
	public float GetPoint(float p, float q, float r, int seed, AnimationCurve remap)
	{
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

        ret = remap.Evaluate(ret);

		if (viewMask)
			ret = mid;
		
		if (dist > 1)
			return 0;

		return ret;
	}

	public Vector3 GetNormal(float p, float q, float r)
	{
		Vector3 nearestPoint = Vector3.zero;
		Vector3 point = new Vector3(p, q, r);
		float min = 1e20f;

		foreach (var dp in densityPoints)
		{
			float dst = Vector3.SqrMagnitude(dp.position - point);
			if (dst < min)
			{
				nearestPoint = dp.position;
				min = dst;
			}
		}

		return (point - nearestPoint).normalized;
	}
	
	public void GenNoisePart(byte[] fileBytes, float part, int numThreads, bool computeNormal)
	{
		int fromX = (int)(textureSize / (numThreads) * part);
		int toX = (int)(textureSize / (numThreads) * (part + 1));

		Debug.Log("Thread started from " + fromX + " to " + toX + ", part: " + part);

		AnimationCurve threadSafeRemap = new AnimationCurve(remapNoise.keys);
		
		for (int x = fromX; x < toX; x++)
		{
			for (int y = 0; y < textureSize; y++)
				for (int z = 0; z < textureSize; z++)
					for (int i = 0; i < channels; i++)
					{
						int cx = x * textureSize * textureSize;
						int cy = y * textureSize;
						int cz = z;
						int ci = (cx + cy + cz) * ((computeNormal) ? 4 : channels) + i;
						float p = (x - textureSize / 2) / scale + offset.x;
						float q = (y - textureSize / 2) / scale + offset.y;
						float r = (z - textureSize / 2) / scale + offset.z;
						float f = GetPoint(p, q, r, i * 183, threadSafeRemap);
						
						if (f < cutoff)
							fileBytes[ci] = 0;
						else
						{
							int val = Mathf.FloorToInt(Mathf.Clamp(f * 255f, 0, 255));
							fileBytes[ci] = (byte)val;
						}

						if (computeNormal)
						{
							Vector3 norm = GetNormal(p, q, r);
							fileBytes[ci + 1] = (byte)Mathf.FloorToInt(norm.x * 255f);
							fileBytes[ci + 2] = (byte)Mathf.FloorToInt(norm.y * 255f);
							fileBytes[ci + 3] = (byte)Mathf.FloorToInt(norm.z * 255f);
						}
					}
		}

		Debug.Log("Thread finished from " + fromX + " to " + toX + ", part: " + part);
	}

}
