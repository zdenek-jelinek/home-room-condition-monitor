using NUnit.Framework;
using Rcm.Connector.Api.Status;
using Rcm.Connector.MindSphere;
using System;

namespace Tests
{
    [TestFixture]
    public class MindSphereConnectionStatusAccessorTests
    {
        [Test]
        public void ConnectionIsConnectedWhenOnboardedAndHasUptime()
        {
            // given
            var uptime = new TimeSpan(10, 5, 8, 14);
            var connectionStatus = new StubMindSphereConnectionStatus { IsOnboarded = true, Uptime = uptime };

            var statusAccessor = new MindSphereConnectionStatusAccessor(connectionStatus);

            // when
            var status = statusAccessor.GetStatus();

            // then
            Assert.IsInstanceOf<ConnectionStatus.Connected>(status);
            Assert.AreEqual(uptime, ((ConnectionStatus.Connected)status).Uptime);
        }

        [Test]
        public void ConnectionIsDisconnectedWhenOnboardedAndHasNullUptime()
        {
            // given
            var connectionStatus = new StubMindSphereConnectionStatus { IsOnboarded = true, Uptime = null };

            var statusAccessor = new MindSphereConnectionStatusAccessor(connectionStatus);

            // when
            var status = statusAccessor.GetStatus();

            // then
            Assert.IsInstanceOf<ConnectionStatus.Disconnected>(status);
        }

        [Test]
        public void ConnectionIsNotEnabledWhenNotOnboarded()
        {
            // given
            var connectionStatus = new StubMindSphereConnectionStatus { IsOnboarded = false };

            var statusAccessor = new MindSphereConnectionStatusAccessor(connectionStatus);

            // when
            var status = statusAccessor.GetStatus();

            // then
            Assert.IsInstanceOf<ConnectionStatus.NotEnabled>(status);
        }

        private class StubMindSphereConnectionStatus : IMindSphereConnectionStatus
        {
            public bool IsOnboarded { get; set; }
            public TimeSpan? Uptime { get; set; }
        }
    }
}