﻿
using CustomComponents;

namespace MechEngineer
{
    [CustomComponent("ArmActuator")]
    public class ArmActuator : SimpleCustomComponent
    {
        public float? AccuracyBonus;
        public TypeDef Type = TypeDef.Hand;

        public enum TypeDef
        {
            Hand, // Hand = default
            Lower,
            Upper,
        }

        public override string ToString()
        {
            return $"ArmActuator: {Type}+{(AccuracyBonus.HasValue ? AccuracyBonus.Value : 0f)}";
        }
    }
}