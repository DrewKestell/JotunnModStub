using UnityEngine;

public class SE_Cozy : SE_Stats
{
	[Header("__SE_Cozy__")]
	public float m_delay = 10f;

	public string m_statusEffect = "";

	private int m_comfortLevel;

	private float m_updateTimer;

	public override void Setup(Character character)
	{
		base.Setup(character);
		m_character.Message(MessageHud.MessageType.Center, "$se_resting_start");
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (m_time > m_delay)
		{
			m_character.GetSEMan().AddStatusEffect(m_statusEffect, resetTime: true);
		}
	}

	public override string GetIconText()
	{
		Player player = m_character as Player;
		return Localization.instance.Localize("$se_rested_comfort:" + player.GetComfortLevel());
	}
}
