using System;
using Microsoft.Xrm.Sdk;

namespace FireSystemEventMonitor.Dataverse.Plugin
{
    /// <summary>
    /// Ensures that a high-priority Dynamics 365 Customer Engagement case has
    /// a follow-up deadline before the Dataverse operation is committed.
    /// </summary>
    public sealed class EnsureHighPriorityFollowUpPlugin : IPlugin
    {
        internal const string CaseTable = "incident";
        internal const string PriorityColumn = "prioritycode";
        internal const string FollowUpColumn = "followupby";
        internal const string PreImageAlias = "PreImage";
        internal const int HighPriorityValue = 1;
        internal const int FollowUpHours = 4;

        /// <summary>
        /// Applies the case follow-up rule to the current Dataverse execution context.
        /// </summary>
        /// <param name="serviceProvider">
        /// Provider for the Dataverse execution context and tracing service.
        /// </param>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context =
                (IPluginExecutionContext)serviceProvider.GetService(
                    typeof(IPluginExecutionContext)
                );

            if (context == null)
            {
                throw new InvalidPluginExecutionException(
                    "Dataverse did not provide a plug-in execution context."
                );
            }

            if (context.Depth > 1)
            {
                Trace(tracingService, "Skipped recursive execution at depth {0}.", context.Depth);
                return;
            }

            if (!string.Equals(
                    context.PrimaryEntityName,
                    CaseTable,
                    StringComparison.OrdinalIgnoreCase
                )
                || (!string.Equals(context.MessageName, "Create", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        context.MessageName,
                        "Update",
                        StringComparison.OrdinalIgnoreCase
                    )))
            {
                Trace(
                    tracingService,
                    "Skipped unsupported message {0} for table {1}.",
                    context.MessageName,
                    context.PrimaryEntityName
                );
                return;
            }

            if (!context.InputParameters.Contains("Target")
                || !(context.InputParameters["Target"] is Entity))
            {
                Trace(tracingService, "Skipped because the Target entity was not supplied.");
                return;
            }

            var target = (Entity)context.InputParameters["Target"];
            if (!string.Equals(target.LogicalName, CaseTable, StringComparison.OrdinalIgnoreCase))
            {
                Trace(
                    tracingService,
                    "Skipped Target table {0}.",
                    target.LogicalName
                );
                return;
            }

            var preImage = GetPreImage(context);
            var priority = ReadOptionSet(target, preImage, PriorityColumn);
            if (!priority.HasValue || priority.Value != HighPriorityValue)
            {
                Trace(tracingService, "No high-priority follow-up rule was required.");
                return;
            }

            var existingFollowUp = ReadDateTime(target, preImage, FollowUpColumn);
            if (existingFollowUp.HasValue)
            {
                Trace(tracingService, "Preserved the existing case follow-up deadline.");
                return;
            }

            target[FollowUpColumn] = context.OperationCreatedOn
                .ToUniversalTime()
                .AddHours(FollowUpHours);
            Trace(
                tracingService,
                "Assigned a {0}-hour follow-up deadline to the high-priority case.",
                FollowUpHours
            );
        }

        private static Entity GetPreImage(IPluginExecutionContext context)
        {
            if (context.PreEntityImages == null
                || !context.PreEntityImages.Contains(PreImageAlias))
            {
                return null;
            }

            return context.PreEntityImages[PreImageAlias];
        }

        private static int? ReadOptionSet(Entity target, Entity preImage, string column)
        {
            var targetValue = target.GetAttributeValue<OptionSetValue>(column);
            if (targetValue != null)
            {
                return targetValue.Value;
            }

            var preImageValue = preImage == null
                ? null
                : preImage.GetAttributeValue<OptionSetValue>(column);
            return preImageValue == null ? (int?)null : preImageValue.Value;
        }

        private static DateTime? ReadDateTime(Entity target, Entity preImage, string column)
        {
            if (target.Contains(column))
            {
                return target.GetAttributeValue<DateTime?>(column);
            }

            return preImage == null
                ? (DateTime?)null
                : preImage.GetAttributeValue<DateTime?>(column);
        }

        private static void Trace(
            ITracingService tracingService,
            string format,
            params object[] arguments
        )
        {
            if (tracingService != null)
            {
                tracingService.Trace(format, arguments);
            }
        }
    }
}
