using System.Collections.Generic;
using UnityEngine;

public class ZDOPool
{
	private static int BATCH_SIZE = 64;

	private static Stack<ZDO> m_free = new Stack<ZDO>();

	private static int m_active = 0;

	public static ZDO Create(ZDOMan man, ZDOID id, Vector3 position)
	{
		ZDO zDO = Get();
		zDO.Initialize(man, id, position);
		return zDO;
	}

	public static ZDO Create(ZDOMan man)
	{
		ZDO zDO = Get();
		zDO.Initialize(man);
		return zDO;
	}

	public static void Release(Dictionary<ZDOID, ZDO> objects)
	{
		foreach (ZDO value in objects.Values)
		{
			Release(value);
		}
	}

	public static void Release(ZDO zdo)
	{
		zdo.Reset();
		m_free.Push(zdo);
		m_active--;
	}

	private static ZDO Get()
	{
		if (m_free.Count <= 0)
		{
			for (int i = 0; i < BATCH_SIZE; i++)
			{
				ZDO item = new ZDO();
				m_free.Push(item);
			}
		}
		m_active++;
		return m_free.Pop();
	}

	public static int GetPoolSize()
	{
		return m_free.Count;
	}

	public static int GetPoolActive()
	{
		return m_active;
	}

	public static int GetPoolTotal()
	{
		return m_active + m_free.Count;
	}
}
