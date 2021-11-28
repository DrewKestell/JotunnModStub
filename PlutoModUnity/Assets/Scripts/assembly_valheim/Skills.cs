using System;
using System.Collections.Generic;
using UnityEngine;

public class Skills : MonoBehaviour
{
	public enum SkillType
	{
		None = 0,
		Swords = 1,
		Knives = 2,
		Clubs = 3,
		Polearms = 4,
		Spears = 5,
		Blocking = 6,
		Axes = 7,
		Bows = 8,
		FireMagic = 9,
		FrostMagic = 10,
		Unarmed = 11,
		Pickaxes = 12,
		WoodCutting = 13,
		Jump = 100,
		Sneak = 101,
		Run = 102,
		Swim = 103,
		Ride = 110,
		All = 999
	}

	[Serializable]
	public class SkillDef
	{
		public SkillType m_skill = SkillType.Swords;

		public Sprite m_icon;

		public string m_description = "";

		public float m_increseStep = 1f;
	}

	public class Skill
	{
		public SkillDef m_info;

		public float m_level;

		public float m_accumulator;

		public Skill(SkillDef info)
		{
			m_info = info;
		}

		public bool Raise(float factor)
		{
			if (m_level >= 100f)
			{
				return false;
			}
			float num = m_info.m_increseStep * factor;
			m_accumulator += num;
			float nextLevelRequirement = GetNextLevelRequirement();
			if (m_accumulator >= nextLevelRequirement)
			{
				m_level += 1f;
				m_level = Mathf.Clamp(m_level, 0f, 100f);
				m_accumulator = 0f;
				return true;
			}
			return false;
		}

		private float GetNextLevelRequirement()
		{
			return Mathf.Pow(m_level + 1f, 1.5f) * 0.5f + 0.5f;
		}

		public float GetLevelPercentage()
		{
			if (m_level >= 100f)
			{
				return 0f;
			}
			float nextLevelRequirement = GetNextLevelRequirement();
			return Mathf.Clamp01(m_accumulator / nextLevelRequirement);
		}
	}

	private const int dataVersion = 2;

	private const float randomSkillRange = 0.15f;

	private const float randomSkillMin = 0.4f;

	public const float m_maxSkillLevel = 100f;

	public const float m_skillCurve = 2f;

	public bool m_useSkillCap;

	public float m_totalSkillCap = 600f;

	public List<SkillDef> m_skills = new List<SkillDef>();

	public float m_DeathLowerFactor = 0.25f;

	private Dictionary<SkillType, Skill> m_skillData = new Dictionary<SkillType, Skill>();

	private Player m_player;

	public void Awake()
	{
		m_player = GetComponent<Player>();
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(2);
		pkg.Write(m_skillData.Count);
		foreach (KeyValuePair<SkillType, Skill> skillDatum in m_skillData)
		{
			pkg.Write((int)skillDatum.Value.m_info.m_skill);
			pkg.Write(skillDatum.Value.m_level);
			pkg.Write(skillDatum.Value.m_accumulator);
		}
	}

	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		m_skillData.Clear();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			SkillType skillType = (SkillType)pkg.ReadInt();
			float level = pkg.ReadSingle();
			float accumulator = ((num >= 2) ? pkg.ReadSingle() : 0f);
			if (IsSkillValid(skillType))
			{
				Skill skill = GetSkill(skillType);
				skill.m_level = level;
				skill.m_accumulator = accumulator;
			}
		}
	}

	private bool IsSkillValid(SkillType type)
	{
		return Enum.IsDefined(typeof(SkillType), type);
	}

	public float GetSkillFactor(SkillType skillType)
	{
		if (skillType == SkillType.None)
		{
			return 0f;
		}
		return Mathf.Clamp01(GetSkillLevel(skillType) / 100f);
	}

	public float GetSkillLevel(SkillType skillType)
	{
		if (skillType == SkillType.None)
		{
			return 0f;
		}
		float level = GetSkill(skillType).m_level;
		m_player.GetSEMan().ModifySkillLevel(skillType, ref level);
		return level;
	}

	public void GetRandomSkillRange(out float min, out float max, SkillType skillType)
	{
		float skillFactor = GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		min = Mathf.Clamp01(num - 0.15f);
		max = Mathf.Clamp01(num + 0.15f);
	}

	public float GetRandomSkillFactor(SkillType skillType)
	{
		float skillFactor = GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		float a = Mathf.Clamp01(num - 0.15f);
		float b = Mathf.Clamp01(num + 0.15f);
		return Mathf.Lerp(a, b, UnityEngine.Random.value);
	}

	public void CheatRaiseSkill(string name, float value)
	{
		foreach (SkillType value2 in Enum.GetValues(typeof(SkillType)))
		{
			if (value2.ToString().ToLower() == name.ToLower() && value2 != SkillType.All && value2 != SkillType.FireMagic && value2 != SkillType.FrostMagic)
			{
				Skill skill = GetSkill(value2);
				skill.m_level += value;
				skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
				if (m_useSkillCap)
				{
					RebalanceSkills(value2);
				}
				if (skill.m_info != null)
				{
					m_player.Message(MessageHud.MessageType.TopLeft, "Skill incresed " + skill.m_info.m_skill.ToString() + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
					Console.instance.Print("Skill " + value2.ToString() + " = " + skill.m_level);
				}
				return;
			}
		}
		Console.instance.Print("Skill not found " + name);
	}

	public void CheatResetSkill(string name)
	{
		foreach (SkillType value in Enum.GetValues(typeof(SkillType)))
		{
			if (value.ToString().ToLower() == name.ToLower())
			{
				ResetSkill(value);
				Console.instance.Print("Skill " + value.ToString() + " reset");
				return;
			}
		}
		Console.instance.Print("Skill not found " + name);
	}

	public void ResetSkill(SkillType skillType)
	{
		m_skillData.Remove(skillType);
	}

	public void RaiseSkill(SkillType skillType, float factor = 1f)
	{
		if (skillType == SkillType.None)
		{
			return;
		}
		Skill skill = GetSkill(skillType);
		float level = skill.m_level;
		if (skill.Raise(factor))
		{
			if (m_useSkillCap)
			{
				RebalanceSkills(skillType);
			}
			m_player.OnSkillLevelup(skillType, skill.m_level);
			MessageHud.MessageType type = (((int)level != 0) ? MessageHud.MessageType.TopLeft : MessageHud.MessageType.Center);
			m_player.Message(type, "$msg_skillup $skill_" + skill.m_info.m_skill.ToString().ToLower() + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
			Gogan.LogEvent("Game", "Levelup", skillType.ToString(), (int)skill.m_level);
		}
	}

	private void RebalanceSkills(SkillType skillType)
	{
		if (GetTotalSkill() < m_totalSkillCap)
		{
			return;
		}
		float level = GetSkill(skillType).m_level;
		float num = m_totalSkillCap - level;
		float num2 = 0f;
		foreach (KeyValuePair<SkillType, Skill> skillDatum in m_skillData)
		{
			if (skillDatum.Key != skillType)
			{
				num2 += skillDatum.Value.m_level;
			}
		}
		foreach (KeyValuePair<SkillType, Skill> skillDatum2 in m_skillData)
		{
			if (skillDatum2.Key != skillType)
			{
				skillDatum2.Value.m_level = skillDatum2.Value.m_level / num2 * num;
			}
		}
	}

	public void Clear()
	{
		m_skillData.Clear();
	}

	public void OnDeath()
	{
		LowerAllSkills(m_DeathLowerFactor);
	}

	public void LowerAllSkills(float factor)
	{
		foreach (KeyValuePair<SkillType, Skill> skillDatum in m_skillData)
		{
			float num = skillDatum.Value.m_level * factor;
			skillDatum.Value.m_level -= num;
			skillDatum.Value.m_accumulator = 0f;
		}
		m_player.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered");
	}

	private Skill GetSkill(SkillType skillType)
	{
		if (m_skillData.TryGetValue(skillType, out var value))
		{
			return value;
		}
		value = new Skill(GetSkillDef(skillType));
		m_skillData.Add(skillType, value);
		return value;
	}

	private SkillDef GetSkillDef(SkillType type)
	{
		foreach (SkillDef skill in m_skills)
		{
			if (skill.m_skill == type)
			{
				return skill;
			}
		}
		return null;
	}

	public List<Skill> GetSkillList()
	{
		List<Skill> list = new List<Skill>();
		foreach (KeyValuePair<SkillType, Skill> skillDatum in m_skillData)
		{
			list.Add(skillDatum.Value);
		}
		return list;
	}

	public float GetTotalSkill()
	{
		float num = 0f;
		foreach (KeyValuePair<SkillType, Skill> skillDatum in m_skillData)
		{
			num += skillDatum.Value.m_level;
		}
		return num;
	}

	public float GetTotalSkillCap()
	{
		return m_totalSkillCap;
	}
}
