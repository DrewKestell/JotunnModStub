using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ZNetView : MonoBehaviour
{
	public static long Everybody;

	public bool m_persistent;

	public bool m_distant;

	public ZDO.ObjectType m_type;

	public bool m_syncInitialScale;

	private ZDO m_zdo;

	private Rigidbody m_body;

	private Dictionary<int, RoutedMethodBase> m_functions = new Dictionary<int, RoutedMethodBase>();

	private bool m_ghost;

	public static bool m_useInitZDO;

	public static ZDO m_initZDO;

	public static bool m_forceDisableInit;

	private static bool m_ghostInit;

	private void Awake()
	{
		if (m_forceDisableInit || ZDOMan.instance == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		m_body = GetComponent<Rigidbody>();
		if (m_useInitZDO && m_initZDO == null)
		{
			ZLog.LogWarning("Double ZNetview when initializing object " + base.gameObject.name);
		}
		if (m_initZDO != null)
		{
			m_zdo = m_initZDO;
			m_initZDO = null;
			if (m_zdo.m_type != m_type && m_zdo.IsOwner())
			{
				m_zdo.SetType(m_type);
			}
			if (m_zdo.m_distant != m_distant && m_zdo.IsOwner())
			{
				m_zdo.SetDistant(m_distant);
			}
			if (m_syncInitialScale)
			{
				Vector3 vec = m_zdo.GetVec3("scale", base.transform.localScale);
				base.transform.localScale = vec;
			}
			if ((bool)m_body)
			{
				m_body.Sleep();
			}
		}
		else
		{
			string prefabName = GetPrefabName();
			m_zdo = ZDOMan.instance.CreateNewZDO(base.transform.position);
			m_zdo.m_persistent = m_persistent;
			m_zdo.m_type = m_type;
			m_zdo.m_distant = m_distant;
			m_zdo.SetPrefab(prefabName.GetStableHashCode());
			m_zdo.SetRotation(base.transform.rotation);
			if (m_syncInitialScale)
			{
				m_zdo.Set("scale", base.transform.localScale);
			}
			if (m_ghostInit)
			{
				m_ghost = true;
				return;
			}
		}
		ZNetScene.instance.AddInstance(m_zdo, this);
	}

	public void SetLocalScale(Vector3 scale)
	{
		base.transform.localScale = scale;
		if (m_zdo != null && m_syncInitialScale && IsOwner())
		{
			m_zdo.Set("scale", base.transform.localScale);
		}
	}

	private void OnDestroy()
	{
		_ = (bool)ZNetScene.instance;
	}

	public void SetPersistent(bool persistent)
	{
		m_zdo.m_persistent = persistent;
	}

	public string GetPrefabName()
	{
		return Utils.GetPrefabName(base.gameObject);
	}

	public void Destroy()
	{
		ZNetScene.instance.Destroy(base.gameObject);
	}

	public bool IsOwner()
	{
		return m_zdo.IsOwner();
	}

	public bool HasOwner()
	{
		return m_zdo.HasOwner();
	}

	public void ClaimOwnership()
	{
		if (!IsOwner())
		{
			m_zdo.SetOwner(ZDOMan.instance.GetMyID());
		}
	}

	public ZDO GetZDO()
	{
		return m_zdo;
	}

	public bool IsValid()
	{
		if (m_zdo != null)
		{
			return m_zdo.IsValid();
		}
		return false;
	}

	public void ResetZDO()
	{
		m_zdo = null;
	}

	public void Register(string name, Action<long> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod(f));
	}

	public void Register<T>(string name, Action<long, T> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T>(f));
	}

	public void Register<T, U>(string name, Action<long, T, U> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U>(f));
	}

	public void Register<T, U, V>(string name, Action<long, T, U, V> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V>(f));
	}

	public void Unregister(string name)
	{
		int stableHashCode = name.GetStableHashCode();
		m_functions.Remove(stableHashCode);
	}

	public void HandleRoutedRPC(ZRoutedRpc.RoutedRPCData rpcData)
	{
		if (m_functions.TryGetValue(rpcData.m_methodHash, out var value))
		{
			value.Invoke(rpcData.m_senderPeerID, rpcData.m_parameters);
		}
		else
		{
			ZLog.LogWarning("Failed to find rpc method " + rpcData.m_methodHash);
		}
	}

	public void InvokeRPC(long targetID, string method, params object[] parameters)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(targetID, m_zdo.m_uid, method, parameters);
	}

	public void InvokeRPC(string method, params object[] parameters)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(m_zdo.m_owner, m_zdo.m_uid, method, parameters);
	}

	public static object[] Deserialize(long callerID, ParameterInfo[] paramInfo, ZPackage pkg)
	{
		List<object> parameters = new List<object>();
		parameters.Add(callerID);
		ZRpc.Deserialize(paramInfo, pkg, ref parameters);
		return parameters.ToArray();
	}

	public static void StartGhostInit()
	{
		m_ghostInit = true;
	}

	public static void FinishGhostInit()
	{
		m_ghostInit = false;
	}
}
