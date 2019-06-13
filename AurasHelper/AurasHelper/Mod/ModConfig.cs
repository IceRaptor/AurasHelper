

namespace IttyBittyLivingSpace {

    public class ModConfig {

        public bool Debug = false;
        public bool Trace = false;

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG: {this.Debug}  TRACE: {this.Trace}");
            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
