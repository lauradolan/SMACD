using Newtonsoft.Json;
using Synthesys.SDK;

namespace Synthesys.Plugins.Dummy
{
    // When creating Action-specific reports, they must inherit from this framework class
    public class DummySpecificReport : ExtensionReport
    {
        public string DummyString { get; set; }
        public DummyDataClass Data { get; set; }

        public override string ReportSummaryName => typeof(DummyReportSummary).FullName;
        public override string ReportViewName => typeof(DummyReportView).FullName;

        public override string GetReportContent()
        {
            return "This is a dummy component used for testing.\n" +
                   "Dummy String Outer:" + DummyString + "\n" +
                   "Dummy String Inner:" + Data.DummyString + "\n" +
                   "Dummy Double:" + Data.DummyDouble;
        }

        protected override ExtensionReport DeserializeFromString(string serializedData) =>
            JsonConvert.DeserializeObject<DummySpecificReport>(serializedData);
    }
}