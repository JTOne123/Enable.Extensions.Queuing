using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.TestUtils;
using Xunit;

namespace Enable.Extensions.Queuing.AzureStorage.Tests
{
    public class AzureStorageQueueClientTests : IClassFixture<AzureStorageTestFixture>, IDisposable
    {
        private readonly AzureStorageTestFixture _fixture;

        private readonly string _queueName;

        private readonly IQueueClient _sut;

        private bool _disposed;

        public AzureStorageQueueClientTests(AzureStorageTestFixture fixture)
        {
            _fixture = fixture;

            var options = new AzureStorageQueueClientFactoryOptions
            {
                AccountName = _fixture.AccountName,
                AccountKey = _fixture.AccountKey,
            };

            var queueFactory = new AzureStorageQueueClientFactory(options);

            _queueName = Guid.NewGuid().ToString();

            _sut = queueFactory.GetQueueReference(_queueName);

            _sut.Clear().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task EnqueueAsync_CanInvoke()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);
        }

        [Fact]
        public async Task DequeueAsync_CanInvoke()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Null(message);
        }

        [Fact]
        public async Task DequeueAsync_ReturnsEnqueuedMessage()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(message);
        }

        [Fact]
        public async Task DequeueAsync_CanDeserializeMessage()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Equal(content, message.GetBody<string>());
        }

        [Fact]
        public async Task AbandonAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.AbandonAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task CompleteAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.CompleteAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task RegisterMessageHandler_CanInvoke()
        {
            // Act
            await _sut.RegisterMessageHandler(
                (message, cancellationToken) => throw new Exception("There should be no messages to process."));
        }

        [Fact]
        public async Task RegisterMessageHandler_MessageHandlerInvoked()
        {
            // Arrange
            var evt = new ManualResetEvent(false);

            Task handler(IQueueMessage message, CancellationToken cancellationToken)
            {
                evt.Set();
                return Task.CompletedTask;
            }

            await _sut.RegisterMessageHandler(handler);

            // Act
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            // Assert
            Assert.True(evt.WaitOne(1000));
        }

        [Fact]
        public async Task RenewLockAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.RenewLockAsync(message, CancellationToken.None);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    // Make a best effort to remove our temporary test queue.
                    _fixture.DeleteQueue(_queueName)
                        .GetAwaiter()
                        .GetResult();
                }
                catch
                {
                }

                if (disposing)
                {
                    _sut.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
