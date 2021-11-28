using System;
using System.Collections.Generic;
using UnityEngine;

public class HitData
{
	[Flags]
	public enum DamageType
	{
		Blunt = 0x1,
		Slash = 0x2,
		Pierce = 0x4,
		Chop = 0x8,
		Pickaxe = 0x10,
		Fire = 0x20,
		Frost = 0x40,
		Lightning = 0x80,
		Poison = 0x100,
		Spirit = 0x200,
		Physical = 0x1F,
		Elemental = 0xE0
	}

	public enum DamageModifier
	{
		Normal,
		Resistant,
		Weak,
		Immune,
		Ignore,
		VeryResistant,
		VeryWeak
	}

	[Serializable]
	public struct DamageModPair
	{
		public DamageType m_type;

		public DamageModifier m_modifier;
	}

	[Serializable]
	public struct DamageModifiers
	{
		public DamageModifier m_blunt;

		public DamageModifier m_slash;

		public DamageModifier m_pierce;

		public DamageModifier m_chop;

		public DamageModifier m_pickaxe;

		public DamageModifier m_fire;

		public DamageModifier m_frost;

		public DamageModifier m_lightning;

		public DamageModifier m_poison;

		public DamageModifier m_spirit;

		public DamageModifiers Clone()
		{
			return (DamageModifiers)MemberwiseClone();
		}

		public void Apply(List<DamageModPair> modifiers)
		{
			foreach (DamageModPair modifier in modifiers)
			{
				switch (modifier.m_type)
				{
				case DamageType.Blunt:
					ApplyIfBetter(ref m_blunt, modifier.m_modifier);
					break;
				case DamageType.Slash:
					ApplyIfBetter(ref m_slash, modifier.m_modifier);
					break;
				case DamageType.Pierce:
					ApplyIfBetter(ref m_pierce, modifier.m_modifier);
					break;
				case DamageType.Chop:
					ApplyIfBetter(ref m_chop, modifier.m_modifier);
					break;
				case DamageType.Pickaxe:
					ApplyIfBetter(ref m_pickaxe, modifier.m_modifier);
					break;
				case DamageType.Fire:
					ApplyIfBetter(ref m_fire, modifier.m_modifier);
					break;
				case DamageType.Frost:
					ApplyIfBetter(ref m_frost, modifier.m_modifier);
					break;
				case DamageType.Lightning:
					ApplyIfBetter(ref m_lightning, modifier.m_modifier);
					break;
				case DamageType.Poison:
					ApplyIfBetter(ref m_poison, modifier.m_modifier);
					break;
				case DamageType.Spirit:
					ApplyIfBetter(ref m_spirit, modifier.m_modifier);
					break;
				}
			}
		}

		public DamageModifier GetModifier(DamageType type)
		{
			switch (type)
			{
			case DamageType.Blunt:
				return m_blunt;
			case DamageType.Slash:
				return m_slash;
			case DamageType.Pierce:
				return m_pierce;
			case DamageType.Chop:
				return m_chop;
			case DamageType.Pickaxe:
				return m_pickaxe;
			case DamageType.Fire:
				return m_fire;
			case DamageType.Frost:
				return m_frost;
			case DamageType.Lightning:
				return m_lightning;
			case DamageType.Poison:
				return m_poison;
			case DamageType.Spirit:
				return m_spirit;
			default:
				return DamageModifier.Normal;
			}
		}

		private void ApplyIfBetter(ref DamageModifier original, DamageModifier mod)
		{
			if (ShouldOverride(original, mod))
			{
				original = mod;
			}
		}

		private bool ShouldOverride(DamageModifier a, DamageModifier b)
		{
			if (a == DamageModifier.Ignore)
			{
				return false;
			}
			if (b == DamageModifier.Immune)
			{
				return true;
			}
			if (a == DamageModifier.VeryResistant && b == DamageModifier.Resistant)
			{
				return false;
			}
			if (a == DamageModifier.VeryWeak && b == DamageModifier.Weak)
			{
				return false;
			}
			if ((a == DamageModifier.Resistant || a == DamageModifier.VeryResistant || a == DamageModifier.Immune) && (b == DamageModifier.Weak || b == DamageModifier.VeryWeak))
			{
				return false;
			}
			return true;
		}

		public void Print()
		{
			ZLog.Log("m_blunt " + m_blunt);
			ZLog.Log("m_slash " + m_slash);
			ZLog.Log("m_pierce " + m_pierce);
			ZLog.Log("m_chop " + m_chop);
			ZLog.Log("m_pickaxe " + m_pickaxe);
			ZLog.Log("m_fire " + m_fire);
			ZLog.Log("m_frost " + m_frost);
			ZLog.Log("m_lightning " + m_lightning);
			ZLog.Log("m_poison " + m_poison);
			ZLog.Log("m_spirit " + m_spirit);
		}
	}

	[Serializable]
	public struct DamageTypes
	{
		public float m_damage;

		public float m_blunt;

		public float m_slash;

		public float m_pierce;

		public float m_chop;

		public float m_pickaxe;

		public float m_fire;

		public float m_frost;

		public float m_lightning;

		public float m_poison;

		public float m_spirit;

		public bool HaveDamage()
		{
			if (!(m_damage > 0f) && !(m_blunt > 0f) && !(m_slash > 0f) && !(m_pierce > 0f) && !(m_chop > 0f) && !(m_pickaxe > 0f) && !(m_fire > 0f) && !(m_frost > 0f) && !(m_lightning > 0f) && !(m_poison > 0f))
			{
				return m_spirit > 0f;
			}
			return true;
		}

		public float GetTotalPhysicalDamage()
		{
			return m_blunt + m_slash + m_pierce;
		}

		public float GetTotalStaggerDamage()
		{
			return m_blunt + m_slash + m_pierce + m_lightning;
		}

		public float GetTotalBlockableDamage()
		{
			return m_blunt + m_slash + m_pierce + m_fire + m_frost + m_lightning + m_poison + m_spirit;
		}

		public float GetTotalElementalDamage()
		{
			return m_fire + m_frost + m_lightning;
		}

		public float GetTotalDamage()
		{
			return m_damage + m_blunt + m_slash + m_pierce + m_chop + m_pickaxe + m_fire + m_frost + m_lightning + m_poison + m_spirit;
		}

		public DamageTypes Clone()
		{
			return (DamageTypes)MemberwiseClone();
		}

		public void Add(DamageTypes other, int multiplier = 1)
		{
			m_damage += other.m_damage * (float)multiplier;
			m_blunt += other.m_blunt * (float)multiplier;
			m_slash += other.m_slash * (float)multiplier;
			m_pierce += other.m_pierce * (float)multiplier;
			m_chop += other.m_chop * (float)multiplier;
			m_pickaxe += other.m_pickaxe * (float)multiplier;
			m_fire += other.m_fire * (float)multiplier;
			m_frost += other.m_frost * (float)multiplier;
			m_lightning += other.m_lightning * (float)multiplier;
			m_poison += other.m_poison * (float)multiplier;
			m_spirit += other.m_spirit * (float)multiplier;
		}

		public void Modify(float multiplier)
		{
			m_damage *= multiplier;
			m_blunt *= multiplier;
			m_slash *= multiplier;
			m_pierce *= multiplier;
			m_chop *= multiplier;
			m_pickaxe *= multiplier;
			m_fire *= multiplier;
			m_frost *= multiplier;
			m_lightning *= multiplier;
			m_poison *= multiplier;
			m_spirit *= multiplier;
		}

		public static float ApplyArmor(float dmg, float ac)
		{
			float result = Mathf.Clamp01(dmg / (ac * 4f)) * dmg;
			if (ac < dmg / 2f)
			{
				result = dmg - ac;
			}
			return result;
		}

		public void ApplyArmor(float ac)
		{
			if (!(ac <= 0f))
			{
				float num = m_blunt + m_slash + m_pierce + m_fire + m_frost + m_lightning + m_poison + m_spirit;
				if (!(num <= 0f))
				{
					float num2 = ApplyArmor(num, ac) / num;
					m_blunt *= num2;
					m_slash *= num2;
					m_pierce *= num2;
					m_fire *= num2;
					m_frost *= num2;
					m_lightning *= num2;
					m_poison *= num2;
					m_spirit *= num2;
				}
			}
		}

		private string DamageRange(float damage, float minFactor, float maxFactor)
		{
			int num = Mathf.RoundToInt(damage * minFactor);
			int num2 = Mathf.RoundToInt(damage * maxFactor);
			return "<color=orange>" + Mathf.RoundToInt(damage) + "</color> <color=yellow>(" + num.ToString() + "-" + num2.ToString() + ") </color>";
		}

		public string GetTooltipString(Skills.SkillType skillType = Skills.SkillType.None)
		{
			if (Player.m_localPlayer == null)
			{
				return "";
			}
			Player.m_localPlayer.GetSkills().GetRandomSkillRange(out var min, out var max, skillType);
			string text = "";
			if (m_damage != 0f)
			{
				text = text + "\n$inventory_damage: " + DamageRange(m_damage, min, max);
			}
			if (m_blunt != 0f)
			{
				text = text + "\n$inventory_blunt: " + DamageRange(m_blunt, min, max);
			}
			if (m_slash != 0f)
			{
				text = text + "\n$inventory_slash: " + DamageRange(m_slash, min, max);
			}
			if (m_pierce != 0f)
			{
				text = text + "\n$inventory_pierce: " + DamageRange(m_pierce, min, max);
			}
			if (m_fire != 0f)
			{
				text = text + "\n$inventory_fire: " + DamageRange(m_fire, min, max);
			}
			if (m_frost != 0f)
			{
				text = text + "\n$inventory_frost: " + DamageRange(m_frost, min, max);
			}
			if (m_lightning != 0f)
			{
				text = text + "\n$inventory_lightning: " + DamageRange(m_lightning, min, max);
			}
			if (m_poison != 0f)
			{
				text = text + "\n$inventory_poison: " + DamageRange(m_poison, min, max);
			}
			if (m_spirit != 0f)
			{
				text = text + "\n$inventory_spirit: " + DamageRange(m_spirit, min, max);
			}
			return text;
		}

		public string GetTooltipString()
		{
			string text = "";
			if (m_damage != 0f)
			{
				text = text + "\n$inventory_damage: <color=yellow>" + m_damage + "</color>";
			}
			if (m_blunt != 0f)
			{
				text = text + "\n$inventory_blunt: <color=yellow>" + m_blunt + "</color>";
			}
			if (m_slash != 0f)
			{
				text = text + "\n$inventory_slash: <color=yellow>" + m_slash + "</color>";
			}
			if (m_pierce != 0f)
			{
				text = text + "\n$inventory_pierce: <color=yellow>" + m_pierce + "</color>";
			}
			if (m_fire != 0f)
			{
				text = text + "\n$inventory_fire: <color=yellow>" + m_fire + "</color>";
			}
			if (m_frost != 0f)
			{
				text = text + "\n$inventory_frost: <color=yellow>" + m_frost + "</color>";
			}
			if (m_lightning != 0f)
			{
				text = text + "\n$inventory_lightning: <color=yellow>" + m_frost + "</color>";
			}
			if (m_poison != 0f)
			{
				text = text + "\n$inventory_poison: <color=yellow>" + m_poison + "</color>";
			}
			if (m_spirit != 0f)
			{
				text = text + "\n$inventory_spirit: <color=yellow>" + m_spirit + "</color>";
			}
			return text;
		}
	}

	public DamageTypes m_damage;

	public int m_toolTier;

	public bool m_dodgeable;

	public bool m_blockable;

	public bool m_ranged;

	public float m_pushForce;

	public float m_backstabBonus = 1f;

	public float m_staggerMultiplier = 1f;

	public Vector3 m_point = Vector3.zero;

	public Vector3 m_dir = Vector3.zero;

	public string m_statusEffect = "";

	public ZDOID m_attacker = ZDOID.None;

	public Skills.SkillType m_skill;

	public Collider m_hitCollider;

	public HitData Clone()
	{
		return (HitData)MemberwiseClone();
	}

	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(m_damage.m_damage);
		pkg.Write(m_damage.m_blunt);
		pkg.Write(m_damage.m_slash);
		pkg.Write(m_damage.m_pierce);
		pkg.Write(m_damage.m_chop);
		pkg.Write(m_damage.m_pickaxe);
		pkg.Write(m_damage.m_fire);
		pkg.Write(m_damage.m_frost);
		pkg.Write(m_damage.m_lightning);
		pkg.Write(m_damage.m_poison);
		pkg.Write(m_damage.m_spirit);
		pkg.Write(m_toolTier);
		pkg.Write(m_pushForce);
		pkg.Write(m_backstabBonus);
		pkg.Write(m_staggerMultiplier);
		pkg.Write(m_dodgeable);
		pkg.Write(m_blockable);
		pkg.Write(m_ranged);
		pkg.Write(m_point);
		pkg.Write(m_dir);
		pkg.Write(m_statusEffect);
		pkg.Write(m_attacker);
		pkg.Write((int)m_skill);
	}

	public void Deserialize(ref ZPackage pkg)
	{
		m_damage.m_damage = pkg.ReadSingle();
		m_damage.m_blunt = pkg.ReadSingle();
		m_damage.m_slash = pkg.ReadSingle();
		m_damage.m_pierce = pkg.ReadSingle();
		m_damage.m_chop = pkg.ReadSingle();
		m_damage.m_pickaxe = pkg.ReadSingle();
		m_damage.m_fire = pkg.ReadSingle();
		m_damage.m_frost = pkg.ReadSingle();
		m_damage.m_lightning = pkg.ReadSingle();
		m_damage.m_poison = pkg.ReadSingle();
		m_damage.m_spirit = pkg.ReadSingle();
		m_toolTier = pkg.ReadInt();
		m_pushForce = pkg.ReadSingle();
		m_backstabBonus = pkg.ReadSingle();
		m_staggerMultiplier = pkg.ReadSingle();
		m_dodgeable = pkg.ReadBool();
		m_blockable = pkg.ReadBool();
		m_ranged = pkg.ReadBool();
		m_point = pkg.ReadVector3();
		m_dir = pkg.ReadVector3();
		m_statusEffect = pkg.ReadString();
		m_attacker = pkg.ReadZDOID();
		m_skill = (Skills.SkillType)pkg.ReadInt();
	}

	public float GetTotalPhysicalDamage()
	{
		return m_damage.GetTotalPhysicalDamage();
	}

	public float GetTotalElementalDamage()
	{
		return m_damage.GetTotalElementalDamage();
	}

	public float GetTotalDamage()
	{
		return m_damage.GetTotalDamage();
	}

	private float ApplyModifier(float baseDamage, DamageModifier mod, ref float normalDmg, ref float resistantDmg, ref float weakDmg, ref float immuneDmg)
	{
		if (mod == DamageModifier.Ignore)
		{
			return 0f;
		}
		float num = baseDamage;
		switch (mod)
		{
		case DamageModifier.Resistant:
			num /= 2f;
			resistantDmg += baseDamage;
			break;
		case DamageModifier.VeryResistant:
			num /= 4f;
			resistantDmg += baseDamage;
			break;
		case DamageModifier.Weak:
			num *= 1.5f;
			weakDmg += baseDamage;
			break;
		case DamageModifier.VeryWeak:
			num *= 2f;
			weakDmg += baseDamage;
			break;
		case DamageModifier.Immune:
			num = 0f;
			immuneDmg += baseDamage;
			break;
		default:
			normalDmg += baseDamage;
			break;
		}
		return num;
	}

	public void ApplyResistance(DamageModifiers modifiers, out DamageModifier significantModifier)
	{
		float normalDmg = m_damage.m_damage;
		float resistantDmg = 0f;
		float weakDmg = 0f;
		float immuneDmg = 0f;
		m_damage.m_blunt = ApplyModifier(m_damage.m_blunt, modifiers.m_blunt, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_slash = ApplyModifier(m_damage.m_slash, modifiers.m_slash, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_pierce = ApplyModifier(m_damage.m_pierce, modifiers.m_pierce, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_chop = ApplyModifier(m_damage.m_chop, modifiers.m_chop, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_pickaxe = ApplyModifier(m_damage.m_pickaxe, modifiers.m_pickaxe, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_fire = ApplyModifier(m_damage.m_fire, modifiers.m_fire, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_frost = ApplyModifier(m_damage.m_frost, modifiers.m_frost, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_lightning = ApplyModifier(m_damage.m_lightning, modifiers.m_lightning, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_poison = ApplyModifier(m_damage.m_poison, modifiers.m_poison, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_spirit = ApplyModifier(m_damage.m_spirit, modifiers.m_spirit, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		significantModifier = DamageModifier.Immune;
		if (immuneDmg >= resistantDmg && immuneDmg >= weakDmg && immuneDmg >= normalDmg)
		{
			significantModifier = DamageModifier.Immune;
		}
		if (normalDmg >= resistantDmg && normalDmg >= weakDmg && normalDmg >= immuneDmg)
		{
			significantModifier = DamageModifier.Normal;
		}
		if (resistantDmg >= weakDmg && resistantDmg >= immuneDmg && resistantDmg >= normalDmg)
		{
			significantModifier = DamageModifier.Resistant;
		}
		if (weakDmg >= resistantDmg && weakDmg >= immuneDmg && weakDmg >= normalDmg)
		{
			significantModifier = DamageModifier.Weak;
		}
	}

	public void ApplyArmor(float ac)
	{
		m_damage.ApplyArmor(ac);
	}

	public void ApplyModifier(float multiplier)
	{
		m_damage.m_blunt *= multiplier;
		m_damage.m_slash *= multiplier;
		m_damage.m_pierce *= multiplier;
		m_damage.m_chop *= multiplier;
		m_damage.m_pickaxe *= multiplier;
		m_damage.m_fire *= multiplier;
		m_damage.m_frost *= multiplier;
		m_damage.m_lightning *= multiplier;
		m_damage.m_poison *= multiplier;
		m_damage.m_spirit *= multiplier;
	}

	public float GetTotalBlockableDamage()
	{
		return m_damage.GetTotalBlockableDamage();
	}

	public void BlockDamage(float damage)
	{
		float totalBlockableDamage = GetTotalBlockableDamage();
		float num = Mathf.Max(0f, totalBlockableDamage - damage);
		if (!(totalBlockableDamage <= 0f))
		{
			float num2 = num / totalBlockableDamage;
			m_damage.m_blunt *= num2;
			m_damage.m_slash *= num2;
			m_damage.m_pierce *= num2;
			m_damage.m_fire *= num2;
			m_damage.m_frost *= num2;
			m_damage.m_lightning *= num2;
			m_damage.m_poison *= num2;
			m_damage.m_spirit *= num2;
		}
	}

	public bool HaveAttacker()
	{
		return !m_attacker.IsNone();
	}

	public Character GetAttacker()
	{
		if (m_attacker.IsNone())
		{
			return null;
		}
		if (ZNetScene.instance == null)
		{
			return null;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(m_attacker);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	public void SetAttacker(Character attacker)
	{
		if ((bool)attacker)
		{
			m_attacker = attacker.GetZDOID();
		}
		else
		{
			m_attacker = ZDOID.None;
		}
	}
}
