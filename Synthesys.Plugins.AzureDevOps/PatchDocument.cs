using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using SMACD.AppTree;

namespace Synthesys.Plugins.AzureDevOps
{
    public static class PatchDocument
    {
        public static JsonPatchOperation Severity(Vulnerability.RiskLevels riskLevel)
        {
            var riskLevelStr = string.Empty;
            switch (riskLevel)
            {
                case Vulnerability.RiskLevels.Informational:
                    riskLevelStr = "4 - Low";
                    break;
                case Vulnerability.RiskLevels.Low:
                    riskLevelStr = "3 - Medium";
                    break;
                case Vulnerability.RiskLevels.Medium:
                    riskLevelStr = "2 - High";
                    break;
                case Vulnerability.RiskLevels.High:
                    riskLevelStr = "1 - Critical";
                    break;
            }

            return CreateBoilerplatePatchOperation(
                "/fields/Microsoft.VSTS.Common.Severity",
                riskLevelStr);
        }

        public static JsonPatchOperation Title(string title) =>
            CreateBoilerplatePatchOperation(
                "/fields/System.Title",
                title);

        public static JsonPatchOperation ReproSteps(string reproSteps) =>
            CreateBoilerplatePatchOperation(
                "/fields/Microsoft.VSTS.TCM.ReproSteps",
                reproSteps);

        public static JsonPatchOperation CreateBoilerplatePatchOperation(string path, string value) =>
            new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = path,
                Value = value
            };
    }
}
