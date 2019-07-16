using System.Threading.Tasks;

namespace SMACD.ScannerEngine.Plugins
{
    public abstract class ScorerPlugin : Plugin
    {
        public abstract Task Score(VulnerabilitySummary summary);
        public abstract Task<bool> Converge(VulnerabilitySummary summary);
    }
}