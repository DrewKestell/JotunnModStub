using System;
using UnityEngine;

public class MapTable : MonoBehaviour
{
	public string m_name = "$piece_maptable";

	public Switch m_readSwitch;

	public Switch m_writeSwitch;

	public EffectList m_writeEffects = new EffectList();

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_nview.Register<ZPackage>("MapData", RPC_MapData);
		Switch readSwitch = m_readSwitch;
		readSwitch.m_onUse = (Switch.Callback)Delegate.Combine(readSwitch.m_onUse, new Switch.Callback(OnRead));
		Switch readSwitch2 = m_readSwitch;
		readSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(readSwitch2.m_onHover, new Switch.TooltipCallback(GetReadHoverText));
		Switch writeSwitch = m_writeSwitch;
		writeSwitch.m_onUse = (Switch.Callback)Delegate.Combine(writeSwitch.m_onUse, new Switch.Callback(OnWrite));
		Switch writeSwitch2 = m_writeSwitch;
		writeSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(writeSwitch2.m_onHover, new Switch.TooltipCallback(GetWriteHoverText));
	}

	public string GetReadHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_readmap ");
	}

	public string GetWriteHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_writemap ");
	}

	public bool OnRead(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null)
		{
			return false;
		}
		_ = Time.realtimeSinceStartup;
		byte[] byteArray = m_nview.GetZDO().GetByteArray("data");
		if (byteArray != null)
		{
			byte[] dataArray = Utils.Decompress(byteArray);
			if (Minimap.instance.AddSharedMapData(dataArray))
			{
				user.Message(MessageHud.MessageType.Center, "$msg_mapsynced");
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, "$msg_alreadysynced");
			}
		}
		else
		{
			user.Message(MessageHud.MessageType.Center, "$msg_mapnodata");
		}
		return false;
	}

	public bool OnWrite(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null)
		{
			return false;
		}
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		byte[] array = m_nview.GetZDO().GetByteArray("data");
		if (array != null)
		{
			array = Utils.Decompress(array);
		}
		ZPackage mapData = GetMapData(array);
		m_nview.InvokeRPC("MapData", mapData);
		user.Message(MessageHud.MessageType.Center, "$msg_mapsaved");
		m_writeEffects.Create(base.transform.position, base.transform.rotation);
		return true;
	}

	private void RPC_MapData(long sender, ZPackage pkg)
	{
		if (m_nview.IsOwner())
		{
			byte[] array = pkg.GetArray();
			m_nview.GetZDO().Set("data", array);
		}
	}

	private ZPackage GetMapData(byte[] currentMapData)
	{
		byte[] array = Utils.Compress(Minimap.instance.GetSharedMapData(currentMapData));
		ZLog.Log("Compressed map data:" + array.Length);
		return new ZPackage(array);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
