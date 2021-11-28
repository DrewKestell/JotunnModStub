using UnityEngine;

internal class Version
{
	public static int m_major = 0;

	public static int m_minor = 205;

	public static int m_patch = 5;

	public static int m_playerVersion = 36;

	public static int[] m_compatiblePlayerVersions = new int[9] { 35, 34, 33, 32, 31, 30, 29, 28, 27 };

	public static int m_worldVersion = 28;

	public static int[] m_compatibleWorldVersions = new int[18]
	{
		27, 26, 25, 24, 23, 22, 21, 20, 19, 18,
		17, 16, 15, 14, 13, 11, 10, 9
	};

	public static int m_worldGenVersion = 1;

	public static string GetVersionString()
	{
		return CombineVersion(m_major, m_minor, m_patch);
	}

	public static bool IsVersionNewer(int major, int minor, int patch)
	{
		if (major > m_major)
		{
			return true;
		}
		if (major == m_major && minor > m_minor)
		{
			return true;
		}
		if (major == m_major && minor == m_minor)
		{
			if (m_patch >= 0)
			{
				return patch > m_patch;
			}
			if (patch >= 0)
			{
				return true;
			}
			return patch < m_patch;
		}
		return false;
	}

	public static string CombineVersion(int major, int minor, int patch)
	{
		if (patch == 0)
		{
			return major + "." + minor;
		}
		if (patch < 0)
		{
			return major + "." + minor + ".rc" + Mathf.Abs(patch);
		}
		return major + "." + minor + "." + patch;
	}

	public static bool IsWorldVersionCompatible(int version)
	{
		if (version == m_worldVersion)
		{
			return true;
		}
		int[] compatibleWorldVersions = m_compatibleWorldVersions;
		foreach (int num in compatibleWorldVersions)
		{
			if (version == num)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsPlayerVersionCompatible(int version)
	{
		if (version == m_playerVersion)
		{
			return true;
		}
		int[] compatiblePlayerVersions = m_compatiblePlayerVersions;
		foreach (int num in compatiblePlayerVersions)
		{
			if (version == num)
			{
				return true;
			}
		}
		return false;
	}
}
