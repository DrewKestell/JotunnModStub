using UnityEngine;

public interface IWaterInteractable
{
	void SetLiquidLevel(float level, LiquidType type);

	Transform GetTransform();
}
