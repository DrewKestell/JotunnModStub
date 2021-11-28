using System;
using System.Globalization;
using UnityEngine;

public class ZLog
{
	public static void Log(object o)
	{
		Debug.Log(string.Concat(DateTime.Now.ToString(CultureInfo.InvariantCulture), ": ", o, "\n"));
	}

	public static void DevLog(object o)
	{
		if (Debug.isDebugBuild)
		{
			Debug.Log(string.Concat(DateTime.Now.ToString(CultureInfo.InvariantCulture), ": ", o, "\n"));
		}
	}

	public static void LogError(object o)
	{
		Debug.LogError(string.Concat(DateTime.Now.ToString(CultureInfo.InvariantCulture), ": ", o, "\n"));
	}

	public static void LogWarning(object o)
	{
		Debug.LogWarning(string.Concat(DateTime.Now.ToString(CultureInfo.InvariantCulture), ": ", o, "\n"));
	}
}
