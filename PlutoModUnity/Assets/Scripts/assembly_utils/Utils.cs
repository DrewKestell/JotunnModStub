using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;

public static class Utils
{
	private static string m_saveDataOverride = null;

	private static Plane[] mainPlanes;

	private static int lastPlaneFrame = -1;

	private static int lastFrameCheck = 0;

	private static Camera lastMainCamera = null;

	public static string GetSaveDataPath()
	{
		if (m_saveDataOverride != null)
		{
			return m_saveDataOverride;
		}
		return Application.persistentDataPath;
	}

	public static void SetSaveDataPath(string path)
	{
		m_saveDataOverride = path;
	}

	public static void ResetSaveDataPath()
	{
		m_saveDataOverride = null;
	}

	public static string GetPrefabName(GameObject gameObject)
	{
		string name = gameObject.name;
		char[] anyOf = new char[2] { '(', ' ' };
		int num = name.IndexOfAny(anyOf);
		if (num != -1)
		{
			return name.Remove(num);
		}
		return name;
	}

	public static bool InsideMainCamera(Bounds bounds)
	{
		Plane[] mainCameraFrustumPlanes = GetMainCameraFrustumPlanes();
		if (mainCameraFrustumPlanes == null)
		{
			return false;
		}
		return GeometryUtility.TestPlanesAABB(mainCameraFrustumPlanes, bounds);
	}

	public static bool InsideMainCamera(BoundingSphere bounds)
	{
		Plane[] mainCameraFrustumPlanes = GetMainCameraFrustumPlanes();
		if (mainCameraFrustumPlanes == null)
		{
			return false;
		}
		for (int i = 0; i < mainCameraFrustumPlanes.Length; i++)
		{
			if (mainCameraFrustumPlanes[i].GetDistanceToPoint(bounds.position) < 0f - bounds.radius)
			{
				return false;
			}
		}
		return true;
	}

	public static Plane[] GetMainCameraFrustumPlanes()
	{
		Camera mainCamera = GetMainCamera();
		if ((bool)mainCamera)
		{
			if (Time.frameCount != lastPlaneFrame)
			{
				mainPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
				lastPlaneFrame = Time.frameCount;
			}
			return mainPlanes;
		}
		return null;
	}

	public static Camera GetMainCamera()
	{
		int frameCount = Time.frameCount;
		if (lastFrameCheck == frameCount)
		{
			return lastMainCamera;
		}
		lastMainCamera = Camera.main;
		lastFrameCheck = frameCount;
		return lastMainCamera;
	}

	public static Color Vec3ToColor(Vector3 c)
	{
		return new Color(c.x, c.y, c.z);
	}

	public static Vector3 ColorToVec3(Color c)
	{
		return new Vector3(c.r, c.g, c.b);
	}

	public static float LerpStep(float l, float h, float v)
	{
		return Mathf.Clamp01((v - l) / (h - l));
	}

	public static float SmoothStep(float p_Min, float p_Max, float p_X)
	{
		float num = Mathf.Clamp01((p_X - p_Min) / (p_Max - p_Min));
		return num * num * (3f - 2f * num);
	}

	public static double LerpStep(double l, double h, double v)
	{
		return Clamp01((v - l) / (h - l));
	}

	public static double Clamp01(double v)
	{
		if (v > 1.0)
		{
			return 1.0;
		}
		if (v < 0.0)
		{
			return 0.0;
		}
		return v;
	}

	public static float Fbm(Vector3 p, int octaves, float lacunarity, float gain)
	{
		return Fbm(new Vector2(p.x, p.z), octaves, lacunarity, gain);
	}

	public static float FbmMaxValue(int octaves, float gain)
	{
		float num = 0f;
		float num2 = 1f;
		for (int i = 0; i < octaves; i++)
		{
			num += num2;
			num2 *= gain;
		}
		return num;
	}

	public static float Fbm(Vector2 p, int octaves, float lacunarity, float gain)
	{
		float num = 0f;
		float num2 = 1f;
		Vector2 vector = p;
		for (int i = 0; i < octaves; i++)
		{
			num += num2 * Mathf.PerlinNoise(vector.x, vector.y);
			num2 *= gain;
			vector *= lacunarity;
		}
		return num;
	}

	public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float smoothTime, float maxSpeed, float deltaTime)
	{
		float num = ((Quaternion.Dot(rot, target) > 0f) ? 1f : (-1f));
		target.x *= num;
		target.y *= num;
		target.z *= num;
		target.w *= num;
		Vector4 normalized = new Vector4(Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, smoothTime, maxSpeed, deltaTime), Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, smoothTime, maxSpeed, deltaTime), Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, smoothTime, maxSpeed, deltaTime), Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, smoothTime, maxSpeed, deltaTime)).normalized;
		float num2 = 1f / Time.deltaTime;
		deriv.x = (normalized.x - rot.x) * num2;
		deriv.y = (normalized.y - rot.y) * num2;
		deriv.z = (normalized.z - rot.z) * num2;
		deriv.w = (normalized.w - rot.w) * num2;
		return new Quaternion(normalized.x, normalized.y, normalized.z, normalized.w);
	}

	public static long GenerateUID()
	{
		IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
		string obj = ((iPGlobalProperties != null && iPGlobalProperties.HostName != null) ? iPGlobalProperties.HostName : "unkown");
		string text = ((iPGlobalProperties != null && iPGlobalProperties.DomainName != null) ? iPGlobalProperties.DomainName : "domain");
		return (long)(obj + ":" + text).GetHashCode() + (long)UnityEngine.Random.Range(1, int.MaxValue);
	}

	public static bool TestPointInViewFrustum(Camera camera, Vector3 worldPos)
	{
		Vector3 vector = camera.WorldToViewportPoint(worldPos);
		if (vector.x >= 0f && vector.x <= 1f && vector.y >= 0f)
		{
			return vector.y <= 1f;
		}
		return false;
	}

	public static Vector3 ParseVector3(string rString)
	{
		string[] array = rString.Substring(1, rString.Length - 2).Split(',');
		float x = float.Parse(array[0]);
		float y = float.Parse(array[1]);
		float z = float.Parse(array[2]);
		return new Vector3(x, y, z);
	}

	public static int GetMinPow2(int val)
	{
		if (val <= 1)
		{
			return 1;
		}
		if (val <= 2)
		{
			return 2;
		}
		if (val <= 4)
		{
			return 4;
		}
		if (val <= 8)
		{
			return 8;
		}
		if (val <= 16)
		{
			return 16;
		}
		if (val <= 32)
		{
			return 32;
		}
		if (val <= 64)
		{
			return 64;
		}
		if (val <= 128)
		{
			return 128;
		}
		if (val <= 256)
		{
			return 256;
		}
		if (val <= 512)
		{
			return 512;
		}
		if (val <= 1024)
		{
			return 1024;
		}
		if (val <= 2048)
		{
			return 2048;
		}
		if (val <= 4096)
		{
			return 4096;
		}
		return 1;
	}

	public static void NormalizeQuaternion(ref Quaternion q)
	{
		float num = 0f;
		for (int i = 0; i < 4; i++)
		{
			num += q[i] * q[i];
		}
		float num2 = 1f / Mathf.Sqrt(num);
		for (int j = 0; j < 4; j++)
		{
			q[j] *= num2;
		}
	}

	public static Vector3 Project(Vector3 v, Vector3 onTo)
	{
		float num = Vector3.Dot(onTo, v);
		return onTo * num;
	}

	public static float Length(float x, float y)
	{
		return Mathf.Sqrt(x * x + y * y);
	}

	public static float DistanceSqr(Vector3 v0, Vector3 v1)
	{
		float num = v1.x - v0.x;
		float num2 = v1.y - v0.y;
		float num3 = v1.z - v0.z;
		return num * num + num2 * num2 + num3 * num3;
	}

	public static float DistanceXZ(Vector3 v0, Vector3 v1)
	{
		float num = v1.x - v0.x;
		float num2 = v1.z - v0.z;
		return Mathf.Sqrt(num * num + num2 * num2);
	}

	public static float LengthXZ(Vector3 v)
	{
		return Mathf.Sqrt(v.x * v.x + v.z * v.z);
	}

	public static Vector3 DirectionXZ(Vector3 dir)
	{
		dir.y = 0f;
		dir.Normalize();
		return dir;
	}

	public static Vector3 Bezier2(Vector3 Start, Vector3 Control, Vector3 End, float delta)
	{
		return (1f - delta) * (1f - delta) * Start + 2f * delta * (1f - delta) * Control + delta * delta * End;
	}

	public static float FixDegAngle(float p_Angle)
	{
		while (p_Angle >= 360f)
		{
			p_Angle -= 360f;
		}
		while (p_Angle < 0f)
		{
			p_Angle += 360f;
		}
		return p_Angle;
	}

	public static float DegDistance(float p_a, float p_b)
	{
		if (p_a == p_b)
		{
			return 0f;
		}
		p_a = FixDegAngle(p_a);
		p_b = FixDegAngle(p_b);
		float num = Mathf.Abs(p_b - p_a);
		if (num > 180f)
		{
			num = Mathf.Abs(num - 360f);
		}
		return num;
	}

	public static float GetYawDeltaAngle(Quaternion q1, Quaternion q2)
	{
		float y = q1.eulerAngles.y;
		float y2 = q2.eulerAngles.y;
		return Mathf.DeltaAngle(y, y2);
	}

	public static float YawFromDirection(Vector3 dir)
	{
		float num = Mathf.Atan2(dir.x, dir.z);
		return FixDegAngle(57.29578f * num);
	}

	public static float DegDirection(float p_a, float p_b)
	{
		if (p_a == p_b)
		{
			return 0f;
		}
		p_a = FixDegAngle(p_a);
		p_b = FixDegAngle(p_b);
		float num = p_a - p_b;
		float num2 = ((num > 0f) ? 1f : (-1f));
		if (Mathf.Abs(num) > 180f)
		{
			num2 *= -1f;
		}
		return num2;
	}

	public static void RotateBodyTo(Rigidbody body, Quaternion rot, float alpha)
	{
	}

	public static string DownloadString(string downloadUrl, int timeoutMS = 5000)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(downloadUrl);
		httpWebRequest.Timeout = timeoutMS;
		httpWebRequest.ReadWriteTimeout = timeoutMS;
		try
		{
			return new StreamReader(((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream()).ReadToEnd();
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while waiting for respons from " + downloadUrl + " -> " + ex.ToString());
			return "";
		}
	}

	public static bool IsEnabledInheirarcy(GameObject go, GameObject root)
	{
		do
		{
			if (!go.activeSelf)
			{
				return false;
			}
			if (go == root)
			{
				return true;
			}
			go = go.transform.parent.gameObject;
		}
		while (go != null);
		return true;
	}

	public static bool IsParent(Transform go, Transform parent)
	{
		do
		{
			if (go == parent)
			{
				return true;
			}
			go = go.parent;
		}
		while (go != null);
		return false;
	}

	public static Transform FindChild(Transform aParent, string aName)
	{
		foreach (Transform item in aParent)
		{
			if (item.name == aName)
			{
				return item;
			}
			Transform transform2 = FindChild(item, aName);
			if (transform2 != null)
			{
				return transform2;
			}
		}
		return null;
	}

	public static void AddToLodgroup(LODGroup lg, GameObject toAdd)
	{
		List<Renderer> list = new List<Renderer>(lg.GetLODs()[0].renderers);
		Renderer[] componentsInChildren = toAdd.GetComponentsInChildren<Renderer>();
		list.AddRange(componentsInChildren);
		lg.GetLODs()[0].renderers = list.ToArray();
	}

	public static void RemoveFromLodgroup(LODGroup lg, GameObject toRemove)
	{
		List<Renderer> list = new List<Renderer>(lg.GetLODs()[0].renderers);
		Renderer[] componentsInChildren = toRemove.GetComponentsInChildren<Renderer>();
		foreach (Renderer item in componentsInChildren)
		{
			list.Remove(item);
		}
		lg.GetLODs()[0].renderers = list.ToArray();
	}

	public static void DrawGizmoCircle(Vector3 center, float radius, int steps)
	{
		float num = (float)Math.PI * 2f / (float)steps;
		Vector3 vector = center + new Vector3(Mathf.Cos(0f) * radius, 0f, Mathf.Sin(0f) * radius);
		Vector3 vector2 = vector;
		for (float num2 = num; num2 <= (float)Math.PI * 2f; num2 += num)
		{
			Vector3 vector3 = center + new Vector3(Mathf.Cos(num2) * radius, 0f, Mathf.Sin(num2) * radius);
			Gizmos.DrawLine(vector3, vector2);
			vector2 = vector3;
		}
		Gizmos.DrawLine(vector2, vector);
	}

	public static void ClampUIToScreen(RectTransform transform)
	{
		Vector3[] array = new Vector3[4];
		transform.GetWorldCorners(array);
		if (!(GetMainCamera() == null))
		{
			float num = 0f;
			float num2 = 0f;
			if (array[2].x > (float)Screen.width)
			{
				num -= array[2].x - (float)Screen.width;
			}
			if (array[0].x < 0f)
			{
				num -= array[0].x;
			}
			if (array[2].y > (float)Screen.height)
			{
				num2 -= array[2].y - (float)Screen.height;
			}
			if (array[0].y < 0f)
			{
				num2 -= array[0].y;
			}
			Vector3 position = transform.position;
			position.x += num;
			position.y += num2;
			transform.position = position;
		}
	}

	public static float Pull(Rigidbody body, Vector3 target, float targetDistance, float speed, float force, float smoothDistance, bool noUpForce = false, bool useForce = false, float power = 1f)
	{
		Vector3 vector = target - body.position;
		float magnitude = vector.magnitude;
		if (magnitude < targetDistance)
		{
			return 0f;
		}
		Vector3 normalized = vector.normalized;
		float num = Mathf.Clamp01((magnitude - targetDistance) / smoothDistance);
		num = (float)Math.Pow(num, power);
		Vector3 vector2 = Vector3.Project(body.velocity, normalized.normalized);
		Vector3 vector3 = normalized.normalized * speed - vector2;
		if (noUpForce && vector3.y > 0f)
		{
			vector3.y = 0f;
		}
		ForceMode mode = (useForce ? ForceMode.Impulse : ForceMode.VelocityChange);
		Vector3 force2 = vector3 * num * Mathf.Clamp01(force);
		body.AddForce(force2, mode);
		return num;
	}

	public static byte[] Compress(byte[] inputArray)
	{
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (GZipStream gZipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Fastest))
			{
				gZipStream.Write(inputArray, 0, inputArray.Length);
			}
			return memoryStream.ToArray();
		}
	}

	public static byte[] Decompress(byte[] inputArray)
	{
		using (MemoryStream stream = new MemoryStream(inputArray))
		{
			using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					gZipStream.CopyTo(memoryStream);
					return memoryStream.ToArray();
				}
			}
		}
	}
}
