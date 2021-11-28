using System.Collections.Generic;

public class SEMan
{
	protected List<StatusEffect> m_statusEffects = new List<StatusEffect>();

	private List<StatusEffect> m_removeStatusEffects = new List<StatusEffect>();

	private int m_statusEffectAttributes;

	private Character m_character;

	private ZNetView m_nview;

	public SEMan(Character character, ZNetView nview)
	{
		m_character = character;
		m_nview = nview;
		m_nview.Register<string, bool>("AddStatusEffect", RPC_AddStatusEffect);
	}

	public void OnDestroy()
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.OnDestroy();
		}
		m_statusEffects.Clear();
	}

	public void ApplyStatusEffectSpeedMods(ref float speed)
	{
		float baseSpeed = speed;
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifySpeed(baseSpeed, ref speed);
		}
	}

	public void ApplyDamageMods(ref HitData.DamageModifiers mods)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyDamageMods(ref mods);
		}
	}

	public void Update(float dt)
	{
		m_statusEffectAttributes = 0;
		int count = m_statusEffects.Count;
		for (int i = 0; i < count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			statusEffect.UpdateStatusEffect(dt);
			if (statusEffect.IsDone())
			{
				m_removeStatusEffects.Add(statusEffect);
			}
			else
			{
				m_statusEffectAttributes |= (int)statusEffect.m_attributes;
			}
		}
		if (m_removeStatusEffects.Count > 0)
		{
			foreach (StatusEffect removeStatusEffect in m_removeStatusEffects)
			{
				removeStatusEffect.Stop();
				m_statusEffects.Remove(removeStatusEffect);
			}
			m_removeStatusEffects.Clear();
		}
		m_nview.GetZDO().Set("seAttrib", m_statusEffectAttributes);
	}

	public StatusEffect AddStatusEffect(string name, bool resetTime = false)
	{
		if (m_nview.IsOwner())
		{
			return Internal_AddStatusEffect(name, resetTime);
		}
		m_nview.InvokeRPC("AddStatusEffect", name, resetTime);
		return null;
	}

	private void RPC_AddStatusEffect(long sender, string name, bool resetTime)
	{
		if (m_nview.IsOwner())
		{
			Internal_AddStatusEffect(name, resetTime);
		}
	}

	private StatusEffect Internal_AddStatusEffect(string name, bool resetTime)
	{
		StatusEffect statusEffect = GetStatusEffect(name);
		if ((bool)statusEffect)
		{
			if (resetTime)
			{
				statusEffect.ResetTime();
			}
			return null;
		}
		StatusEffect statusEffect2 = ObjectDB.instance.GetStatusEffect(name);
		if (statusEffect2 == null)
		{
			return null;
		}
		return AddStatusEffect(statusEffect2);
	}

	public StatusEffect AddStatusEffect(StatusEffect statusEffect, bool resetTime = false)
	{
		StatusEffect statusEffect2 = GetStatusEffect(statusEffect.name);
		if ((bool)statusEffect2)
		{
			if (resetTime)
			{
				statusEffect2.ResetTime();
			}
			return null;
		}
		if (!statusEffect.CanAdd(m_character))
		{
			return null;
		}
		StatusEffect statusEffect3 = statusEffect.Clone();
		m_statusEffects.Add(statusEffect3);
		statusEffect3.Setup(m_character);
		if (m_character.IsPlayer())
		{
			Gogan.LogEvent("Game", "StatusEffect", statusEffect.name, 0L);
		}
		return statusEffect3;
	}

	public bool RemoveStatusEffect(StatusEffect se, bool quiet = false)
	{
		return RemoveStatusEffect(se.name, quiet);
	}

	public bool RemoveStatusEffect(string name, bool quiet = false)
	{
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			if (statusEffect.name == name)
			{
				if (quiet)
				{
					statusEffect.m_stopMessage = "";
				}
				statusEffect.Stop();
				m_statusEffects.Remove(statusEffect);
				return true;
			}
		}
		return false;
	}

	public void RemoveAllStatusEffects(bool quiet = false)
	{
		for (int num = m_statusEffects.Count - 1; num >= 0; num--)
		{
			StatusEffect statusEffect = m_statusEffects[num];
			if (quiet)
			{
				statusEffect.m_stopMessage = "";
			}
			statusEffect.Stop();
			m_statusEffects.Remove(statusEffect);
		}
	}

	public bool HaveStatusEffectCategory(string cat)
	{
		if (cat.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			if (statusEffect.m_category.Length > 0 && statusEffect.m_category == cat)
			{
				return true;
			}
		}
		return false;
	}

	public bool HaveStatusAttribute(StatusEffect.StatusAttribute value)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_nview.IsOwner())
		{
			return ((uint)m_statusEffectAttributes & (uint)value) != 0;
		}
		return ((uint)m_nview.GetZDO().GetInt("seAttrib") & (uint)value) != 0;
	}

	public bool HaveStatusEffect(string name)
	{
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			if (m_statusEffects[i].name == name)
			{
				return true;
			}
		}
		return false;
	}

	public List<StatusEffect> GetStatusEffects()
	{
		return m_statusEffects;
	}

	public StatusEffect GetStatusEffect(string name)
	{
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			if (statusEffect.name == name)
			{
				return statusEffect;
			}
		}
		return null;
	}

	public void GetHUDStatusEffects(List<StatusEffect> effects)
	{
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			if ((bool)statusEffect.m_icon)
			{
				effects.Add(statusEffect);
			}
		}
	}

	public void ModifyNoise(float baseNoise, ref float noise)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyNoise(baseNoise, ref noise);
		}
	}

	public void ModifySkillLevel(Skills.SkillType skill, ref float level)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifySkillLevel(skill, ref level);
		}
	}

	public void ModifyRaiseSkill(Skills.SkillType skill, ref float multiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyRaiseSkill(skill, ref multiplier);
		}
	}

	public void ModifyStaminaRegen(ref float staminaMultiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyStaminaRegen(ref staminaMultiplier);
		}
	}

	public void ModifyHealthRegen(ref float regenMultiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyHealthRegen(ref regenMultiplier);
		}
	}

	public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyMaxCarryWeight(baseLimit, ref limit);
		}
	}

	public void ModifyStealth(float baseStealth, ref float stealth)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyStealth(baseStealth, ref stealth);
		}
	}

	public void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyAttack(skill, ref hitData);
		}
	}

	public void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyRunStaminaDrain(baseDrain, ref drain);
		}
		if (drain < 0f)
		{
			drain = 0f;
		}
	}

	public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyJumpStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void OnDamaged(HitData hit, Character attacker)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.OnDamaged(hit, attacker);
		}
	}
}
