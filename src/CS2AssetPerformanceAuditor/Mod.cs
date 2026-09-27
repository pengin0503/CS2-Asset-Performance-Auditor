#if CS2_GAME_API_AVAILABLE
using Game;
using Game.Modding;
#endif

namespace CS2AssetPerformanceAuditor
{
#if CS2_GAME_API_AVAILABLE
    public sealed class Mod : IMod
    {
        public void OnLoad(UpdateSystem updateSystem)
        {
        }

        public void OnDispose()
        {
        }
    }
#else
    // This marker keeps the project buildable without local game assemblies. A distributable
    // mod build defines CS2_GAME_API_AVAILABLE by supplying CSII_MANAGEDPATH.
    public static class Mod
    {
        public const bool RequiresGameReferences = true;
    }
#endif
}
