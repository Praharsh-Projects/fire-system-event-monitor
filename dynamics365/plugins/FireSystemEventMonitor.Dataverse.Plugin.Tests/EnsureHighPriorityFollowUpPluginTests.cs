using System;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;

namespace FireSystemEventMonitor.Dataverse.Plugin.Tests
{
    public sealed class EnsureHighPriorityFollowUpPluginTests
    {
        private static readonly DateTime OperationTime =
            new DateTime(2026, 7, 27, 8, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CreateHighPriorityCaseAssignsFourHourFollowUp()
        {
            var target = Case(new OptionSetValue(1));
            var context = Context("Create", target, null);

            Execute(context);

            Assert.Equal(
                OperationTime.AddHours(4),
                target.GetAttributeValue<DateTime>("followupby")
            );
        }

        [Fact]
        public void UpdateUsesPreImagePriorityWhenTargetOmitsPriority()
        {
            var target = Case(null);
            var preImage = Case(new OptionSetValue(1));
            var context = Context("Update", target, preImage);

            Execute(context);

            Assert.Equal(
                OperationTime.AddHours(4),
                target.GetAttributeValue<DateTime>("followupby")
            );
        }

        [Fact]
        public void ExistingTargetFollowUpIsPreserved()
        {
            var expected = OperationTime.AddDays(1);
            var target = Case(new OptionSetValue(1));
            target["followupby"] = expected;
            var context = Context("Update", target, null);

            Execute(context);

            Assert.Equal(expected, target.GetAttributeValue<DateTime>("followupby"));
        }

        [Fact]
        public void ExistingPreImageFollowUpIsPreserved()
        {
            var expected = OperationTime.AddDays(1);
            var target = Case(new OptionSetValue(1));
            var preImage = Case(new OptionSetValue(1));
            preImage["followupby"] = expected;
            var context = Context("Update", target, preImage);

            Execute(context);

            Assert.False(target.Contains("followupby"));
        }

        [Fact]
        public void NormalPriorityCaseIsNotModified()
        {
            var target = Case(new OptionSetValue(2));
            var context = Context("Create", target, null);

            Execute(context);

            Assert.False(target.Contains("followupby"));
        }

        [Fact]
        public void UnsupportedMessageIsNotModified()
        {
            var target = Case(new OptionSetValue(1));
            var context = Context("Delete", target, null);

            Execute(context);

            Assert.False(target.Contains("followupby"));
        }

        [Fact]
        public void RecursiveExecutionIsNotModified()
        {
            var target = Case(new OptionSetValue(1));
            var context = Context("Create", target, null, 2);

            Execute(context);

            Assert.False(target.Contains("followupby"));
        }

        [Fact]
        public void MissingExecutionContextFailsClosed()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(provider => provider.GetService(typeof(ITracingService)))
                .Returns(Mock.Of<ITracingService>());
            serviceProvider
                .Setup(provider => provider.GetService(typeof(IPluginExecutionContext)))
                .Returns(null);

            var exception = Assert.Throws<InvalidPluginExecutionException>(
                () => new EnsureHighPriorityFollowUpPlugin().Execute(serviceProvider.Object)
            );

            Assert.Contains("execution context", exception.Message);
        }

        [Fact]
        public void NullServiceProviderIsRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () => new EnsureHighPriorityFollowUpPlugin().Execute(null)
            );
        }

        private static Entity Case(OptionSetValue priority)
        {
            var entity = new Entity("incident");
            if (priority != null)
            {
                entity["prioritycode"] = priority;
            }

            return entity;
        }

        private static IPluginExecutionContext Context(
            string message,
            Entity target,
            Entity preImage,
            int depth = 1
        )
        {
            var inputParameters = new ParameterCollection
            {
                { "Target", target },
            };
            var preImages = new EntityImageCollection();
            if (preImage != null)
            {
                preImages.Add("PreImage", preImage);
            }

            var context = new Mock<IPluginExecutionContext>();
            context.SetupGet(value => value.MessageName).Returns(message);
            context.SetupGet(value => value.PrimaryEntityName).Returns("incident");
            context.SetupGet(value => value.Depth).Returns(depth);
            context.SetupGet(value => value.OperationCreatedOn).Returns(OperationTime);
            context.SetupGet(value => value.InputParameters).Returns(inputParameters);
            context.SetupGet(value => value.PreEntityImages).Returns(preImages);
            return context.Object;
        }

        private static void Execute(IPluginExecutionContext context)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(provider => provider.GetService(typeof(ITracingService)))
                .Returns(Mock.Of<ITracingService>());
            serviceProvider
                .Setup(provider => provider.GetService(typeof(IPluginExecutionContext)))
                .Returns(context);

            new EnsureHighPriorityFollowUpPlugin().Execute(serviceProvider.Object);
        }
    }
}
