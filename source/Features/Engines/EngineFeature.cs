using CustomComponents;
using MechEngineer.Features.Engines.Handler;

namespace MechEngineer.Features.Engines
{
    internal class EngineFeature : Feature<EngineSettings>
    {
        internal static EngineFeature Shared = new EngineFeature();

        internal override EngineSettings Settings => Control.settings.Engine;

        internal static EngineSettings settings => Shared.Settings;

        internal override void SetupFeatureLoaded()
        {
            Validator.RegisterMechValidator(EngineValidation.Shared.CCValidation.ValidateMech, EngineValidation.Shared.CCValidation.ValidateMechCanBeFielded);
        }
    }
}
