using System.Collections.Generic;
using UnityEngine;

public class SE_Stats : StatusEffect
{
	[Header("__SE_Stats__")]
	[Header("HP per tick")]
	public float m_tickInterval;

	public float m_healthPerTickMinHealthPercentage;

	public float m_healthPerTick;

	[Header("Health over time")]
	public float m_healthOverTime;

	public float m_healthOverTimeDuration;

	public float m_healthOverTimeInterval = 5f;

	[Header("Stamina")]
	public float m_staminaOverTime;

	public float m_staminaOverTimeDuration;

	public float m_staminaDrainPerSec;

	public float m_runStaminaDrainModifier;

	public float m_jumpStaminaUseModifier;

	[Header("Regen modifiers")]
	public float m_healthRegenMultiplier = 1f;

	public float m_staminaRegenMultiplier = 1f;

	[Header("Modify raise skill")]
	public Skills.SkillType m_raiseSkill;

	public float m_raiseSkillModifier;

	[Header("Modify skill level")]
	public Skills.SkillType m_skillLevel;

	public float m_skillLevelModifier;

	[Header("Hit modifier")]
	public List<HitData.DamageModPair> m_mods = new List<HitData.DamageModPair>();

	[Header("Attack")]
	public Skills.SkillType m_modifyAttackSkill;

	public float m_damageModifier = 1f;

	[Header("Sneak")]
	public float m_noiseModifier;

	public float m_stealthModifier;

	[Header("Carry weight")]
	public float m_addMaxCarryWeight;

	[Header("Speed")]
	public float m_speedModifier;

	private float m_tickTimer;

	private float m_healthOverTimeTimer;

	private float m_healthOverTimeTicks;

	private float m_healthOverTimeTickHP;

	public override void Setup(Character character)
	{
		base.Setup(character);
		if (m_healthOverTime > 0f && m_healthOverTimeInterval > 0f)
		{
			if (m_healthOverTimeDuration <= 0f)
			{
				m_healthOverTimeDuration = m_ttl;
			}
			m_healthOverTimeTicks = m_healthOverTimeDuration / m_healthOverTimeInterval;
			m_healthOverTimeTickHP = m_healthOverTime / m_healthOverTimeTicks;
		}
		if (m_staminaOverTime > 0f && m_staminaOverTimeDuration <= 0f)
		{
			m_staminaOverTimeDuration = m_ttl;
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (m_tickInterval > 0f)
		{
			m_tickTimer += dt;
			if (m_tickTimer >= m_tickInterval)
			{
				m_tickTimer = 0f;
				if (m_character.GetHealthPercentage() >= m_healthPerTickMinHealthPercentage)
				{
					if (m_healthPerTick > 0f)
					{
						m_character.Heal(m_healthPerTick);
					}
					else
					{
						HitData hitData = new HitData();
						hitData.m_damage.m_damage = 0f - m_healthPerTick;
						hitData.m_point = m_character.GetTopPoint();
						m_character.Damage(hitData);
					}
				}
			}
		}
		if (m_healthOverTimeTicks > 0f)
		{
			m_healthOverTimeTimer += dt;
			if (m_healthOverTimeTimer > m_healthOverTimeInterval)
			{
				m_healthOverTimeTimer = 0f;
				m_healthOverTimeTicks -= 1f;
				m_character.Heal(m_healthOverTimeTickHP);
			}
		}
		if (m_staminaOverTime != 0f && m_time <= m_staminaOverTimeDuration)
		{
			float num = m_staminaOverTimeDuration / dt;
			m_character.AddStamina(m_staminaOverTime / num);
		}
		if (m_staminaDrainPerSec > 0f)
		{
			m_character.UseStamina(m_staminaDrainPerSec * dt);
		}
	}

	public override void ModifyHealthRegen(ref float regenMultiplier)
	{
		if (m_healthRegenMultiplier > 1f)
		{
			regenMultiplier += m_healthRegenMultiplier - 1f;
		}
		else
		{
			regenMultiplier *= m_healthRegenMultiplier;
		}
	}

	public override void ModifyStaminaRegen(ref float staminaRegen)
	{
		if (m_staminaRegenMultiplier > 1f)
		{
			staminaRegen += m_staminaRegenMultiplier - 1f;
		}
		else
		{
			staminaRegen *= m_staminaRegenMultiplier;
		}
	}

	public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
		modifiers.Apply(m_mods);
	}

	public override void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
		if (m_raiseSkill != 0 && (m_raiseSkill == Skills.SkillType.All || m_raiseSkill == skill))
		{
			value += m_raiseSkillModifier;
		}
	}

	public override void ModifySkillLevel(Skills.SkillType skill, ref float value)
	{
		if (m_skillLevel != 0 && (m_skillLevel == Skills.SkillType.All || m_skillLevel == skill))
		{
			value += m_skillLevelModifier;
		}
	}

	public override void ModifyNoise(float baseNoise, ref float noise)
	{
		noise += baseNoise * m_noiseModifier;
	}

	public override void ModifyStealth(float baseStealth, ref float stealth)
	{
		stealth += baseStealth * m_stealthModifier;
	}

	public override void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		limit += m_addMaxCarryWeight;
		if (limit < 0f)
		{
			limit = 0f;
		}
	}

	public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		if (skill == m_modifyAttackSkill || m_modifyAttackSkill == Skills.SkillType.All)
		{
			hitData.m_damage.Modify(m_damageModifier);
		}
	}

	public override void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
		drain += baseDrain * m_runStaminaDrainModifier;
	}

	public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_jumpStaminaUseModifier;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed)
	{
		if (m_character.IsSwiming())
		{
			speed += baseSpeed * m_speedModifier * 0.5f;
		}
		else
		{
			speed += baseSpeed * m_speedModifier;
		}
		if (speed < 0f)
		{
			speed = 0f;
		}
	}

	public override string GetTooltipString()
	{
		string text = "";
		if (m_tooltip.Length > 0)
		{
			text = text + m_tooltip + "\n";
		}
		if (m_jumpStaminaUseModifier != 0f)
		{
			text = text + "$se_jumpstamina: " + (m_jumpStaminaUseModifier * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_runStaminaDrainModifier != 0f)
		{
			text = text + "$se_runstamina: " + (m_runStaminaDrainModifier * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_healthOverTime != 0f)
		{
			text = text + "$se_health: " + m_healthOverTime + "\n";
		}
		if (m_staminaOverTime != 0f)
		{
			text = text + "$se_stamina: " + m_staminaOverTime + "\n";
		}
		if (m_healthRegenMultiplier != 1f)
		{
			text = text + "$se_healthregen " + ((m_healthRegenMultiplier - 1f) * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_staminaRegenMultiplier != 1f)
		{
			text = text + "$se_staminaregen " + ((m_staminaRegenMultiplier - 1f) * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_addMaxCarryWeight != 0f)
		{
			text = text + "$se_max_carryweight " + m_addMaxCarryWeight.ToString("+0;-0") + "\n";
		}
		if (m_mods.Count > 0)
		{
			text += GetDamageModifiersTooltipString(m_mods);
		}
		if (m_noiseModifier != 0f)
		{
			text = text + "$se_noisemod " + (m_noiseModifier * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_stealthModifier != 0f)
		{
			text = text + "$se_sneakmod " + ((0f - m_stealthModifier) * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_speedModifier != 0f)
		{
			text = text + "$item_movement_modifier " + (m_speedModifier * 100f).ToString("+0;-0") + "%\n";
		}
		if (m_skillLevel != 0)
		{
			text += Localization.instance.Localize("$skill_" + m_skillLevel.ToString().ToLower() + " " + m_skillLevelModifier.ToString("+0;-0") + "\n");
		}
		return text;
	}

	public static string GetDamageModifiersTooltipString(List<HitData.DamageModPair> mods)
	{
		if (mods.Count == 0)
		{
			return "";
		}
		string text = "";
		foreach (HitData.DamageModPair mod in mods)
		{
			if (mod.m_modifier != HitData.DamageModifier.Ignore && mod.m_modifier != 0)
			{
				switch (mod.m_modifier)
				{
				case HitData.DamageModifier.Immune:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_immune</color> VS ";
					break;
				case HitData.DamageModifier.Resistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
					break;
				case HitData.DamageModifier.VeryResistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryresistant</color> VS ";
					break;
				case HitData.DamageModifier.Weak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_weak</color> VS ";
					break;
				case HitData.DamageModifier.VeryWeak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryweak</color> VS ";
					break;
				}
				text += "<color=orange>";
				switch (mod.m_type)
				{
				case HitData.DamageType.Blunt:
					text += "$inventory_blunt";
					break;
				case HitData.DamageType.Slash:
					text += "$inventory_slash";
					break;
				case HitData.DamageType.Pierce:
					text += "$inventory_pierce";
					break;
				case HitData.DamageType.Chop:
					text += "$inventory_chop";
					break;
				case HitData.DamageType.Pickaxe:
					text += "$inventory_pickaxe";
					break;
				case HitData.DamageType.Fire:
					text += "$inventory_fire";
					break;
				case HitData.DamageType.Frost:
					text += "$inventory_frost";
					break;
				case HitData.DamageType.Lightning:
					text += "$inventory_lightning";
					break;
				case HitData.DamageType.Poison:
					text += "$inventory_poison";
					break;
				case HitData.DamageType.Spirit:
					text += "$inventory_spirit";
					break;
				}
				text += "</color>";
			}
		}
		return text;
	}
}
