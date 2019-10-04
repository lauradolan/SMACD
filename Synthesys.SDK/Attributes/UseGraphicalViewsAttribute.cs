using System;

namespace Synthesys.SDK.Attributes
{
    /// <summary>
    ///     The TriggeredByAttribute specifies that the Extension is added to the end of the Task Queue when a certain event
    ///     occurs.
    ///     This Attribute can be used multiple times on the same Extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UseGraphicalViewsAttribute : Attribute
    {
        /// <summary>
        ///     Class of Razor component providing a full-page, detailed report
        /// </summary>
        public Type DetailView { get; set; }

        /// <summary>
        ///     Class of Razor component providing a button or similar to showcase key elements about the Extension results
        /// </summary>
        public Type SummaryView { get; set; }

        /// <summary>
        ///     Specify the Razor component names which can be used to show a summary control and/or detailed view
        /// </summary>
        /// <param name="detailView">Class of Razor component providing a full-page, detailed report</param>
        /// <param name="summaryView">Class of Razor component providing a button or similar to showcase key elements about the Extension results</param>
        public UseGraphicalViewsAttribute(Type detailView, Type summaryView)
        {
            DetailView = detailView;
            SummaryView = summaryView;
        }
    }
}