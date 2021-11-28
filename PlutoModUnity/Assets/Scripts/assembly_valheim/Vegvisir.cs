using UnityEngine;

public class Vegvisir : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "$piece_vegvisir";

	public string m_locationName = "";

	public string m_pinName = "Pin";

	public Minimap.PinType m_pinType;

	public string GetHoverText()
	{
		return Localization.instance.Localize(m_name + " " + m_pinName + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_register_location ");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Game.instance.DiscoverClosestLocation(m_locationName, base.transform.position, m_pinName, (int)m_pinType);
		Gogan.LogEvent("Game", "Vegvisir", m_locationName, 0L);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
