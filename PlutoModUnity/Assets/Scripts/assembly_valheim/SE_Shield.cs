using UnityEngine;

public class SE_Shield : StatusEffect
{
	[Header("__SE_Shield__")]
	public float m_absorbDamage = 100f;

	public EffectList m_breakEffects = new EffectList();

	public EffectList m_hitEffects = new EffectList();

	private float m_damage;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override bool IsDone()
	{
		if (m_damage > m_absorbDamage)
		{
			m_breakEffects.Create(m_character.GetCenterPoint(), m_character.transform.rotation, m_character.transform, m_character.GetRadius() * 2f);
			return true;
		}
		return base.IsDone();
	}

	public override void OnDamaged(HitData hit, Character attacker)
	{
		float totalDamage = hit.GetTotalDamage();
		m_damage += totalDamage;
		hit.ApplyModifier(0f);
		m_hitEffects.Create(hit.m_point, Quaternion.identity);
	}
}
