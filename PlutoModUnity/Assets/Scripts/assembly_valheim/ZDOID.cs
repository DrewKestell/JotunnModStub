using System;
using System.IO;

public struct ZDOID : IEquatable<ZDOID>
{
	public static ZDOID None = new ZDOID(0L, 0u);

	private long m_userID;

	private uint m_id;

	private int m_hash;

	public long userID => m_userID;

	public uint id => m_id;

	public ZDOID(BinaryReader reader)
	{
		m_userID = reader.ReadInt64();
		m_id = reader.ReadUInt32();
		m_hash = 0;
	}

	public ZDOID(long userID, uint id)
	{
		m_userID = userID;
		m_id = id;
		m_hash = 0;
	}

	public ZDOID(ZDOID other)
	{
		m_userID = other.m_userID;
		m_id = other.m_id;
		m_hash = other.m_hash;
	}

	public override string ToString()
	{
		return m_userID + ":" + m_id;
	}

	public static bool operator ==(ZDOID a, ZDOID b)
	{
		if (a.m_userID == b.m_userID)
		{
			return a.m_id == b.m_id;
		}
		return false;
	}

	public static bool operator !=(ZDOID a, ZDOID b)
	{
		if (a.m_userID == b.m_userID)
		{
			return a.m_id != b.m_id;
		}
		return true;
	}

	public bool Equals(ZDOID other)
	{
		if (other.m_userID == m_userID)
		{
			return other.m_id == m_id;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is ZDOID zDOID)
		{
			if (zDOID.m_userID == m_userID)
			{
				return zDOID.m_id == m_id;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (m_hash == 0)
		{
			m_hash = m_userID.GetHashCode() ^ m_id.GetHashCode();
		}
		return m_hash;
	}

	public bool IsNone()
	{
		if (m_userID == 0L)
		{
			return m_id == 0;
		}
		return false;
	}
}
