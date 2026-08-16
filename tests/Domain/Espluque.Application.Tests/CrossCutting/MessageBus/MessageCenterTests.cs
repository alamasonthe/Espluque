using Espluque.Application.CrossCutting.MessageBus;
using Espluque.Contracts.CrossCutting;
using Moq;

namespace Espluque.Application.Tests.CrossCutting
{
    public class MessageCenterTests
    {
        [Fact]
        public async Task Register_DoesNotRegisterSameClientTwice()
        {
            MessageCenter messageCenter = new();

            Mock<IMessageClient> client = new();
            Mock<IMessage> message = new();

            client
                .Setup(x => x.HandleAsync(message.Object))
                .Returns(Task.CompletedTask);

            messageCenter.Register(client.Object);
            messageCenter.Register(client.Object);

            await messageCenter.SendAsync(message.Object);

            client.Verify(
                x => x.HandleAsync(message.Object),
                Times.Once);
        }


        [Fact]
        public async Task Unregister_PreventsClientFromReceivingMessages()
        {
            MessageCenter messageCenter = new();

            Mock<IMessageClient> client = new();
            Mock<IMessage> message = new();

            messageCenter.Register(client.Object);
            messageCenter.Unregister(client.Object);

            await messageCenter.SendAsync(message.Object);

            client.Verify(
                x => x.HandleAsync(It.IsAny<IMessage>()),
                Times.Never);
        }


        [Fact]
        public async Task SendAsync_SendsMessageToAllRegisteredClients()
        {
            MessageCenter messageCenter = new();

            Mock<IMessageClient> firstClient = new();
            Mock<IMessageClient> secondClient = new();
            Mock<IMessage> message = new();

            firstClient
                .Setup(x => x.HandleAsync(message.Object))
                .Returns(Task.CompletedTask);

            secondClient
                .Setup(x => x.HandleAsync(message.Object))
                .Returns(Task.CompletedTask);

            messageCenter.Register(firstClient.Object);
            messageCenter.Register(secondClient.Object);

            await messageCenter.SendAsync(message.Object);

            firstClient.Verify(
                x => x.HandleAsync(message.Object),
                Times.Once);

            secondClient.Verify(
                x => x.HandleAsync(message.Object),
                Times.Once);
        }


        [Fact]
        public async Task SendAsync_ContinuesWhenClientThrowsException()
        {
            MessageCenter messageCenter = new();

            Mock<IMessageClient> failingClient = new();
            Mock<IMessageClient> secondClient = new();
            Mock<IMessage> message = new();

            failingClient
                .Setup(x => x.HandleAsync(message.Object))
                .ThrowsAsync(new InvalidOperationException());

            secondClient
                .Setup(x => x.HandleAsync(message.Object))
                .Returns(Task.CompletedTask);

            messageCenter.Register(failingClient.Object);
            messageCenter.Register(secondClient.Object);

            await messageCenter.SendAsync(message.Object);

            failingClient.Verify(
                x => x.HandleAsync(message.Object),
                Times.Once);

            secondClient.Verify(
                x => x.HandleAsync(message.Object),
                Times.Once);
        }


        [Fact]
        public async Task SendAsync_AllowsClientToUnregisterDuringDispatch()
        {
            MessageCenter messageCenter = new();

            Mock<IMessageClient> firstClient = new();
            Mock<IMessageClient> secondClient = new();
            Mock<IMessage> message = new();

            firstClient
                .Setup(x => x.HandleAsync(message.Object))
                .Callback(() => messageCenter.Unregister(firstClient.Object))
                .Returns(Task.CompletedTask);

            secondClient
                .Setup(x => x.HandleAsync(message.Object))
                .Returns(Task.CompletedTask);

            messageCenter.Register(firstClient.Object);
            messageCenter.Register(secondClient.Object);

            await messageCenter.SendAsync(message.Object);
            await messageCenter.SendAsync(message.Object);

            firstClient.Verify(
                x => x.HandleAsync(message.Object),
                Times.Once);

            secondClient.Verify(
                x => x.HandleAsync(message.Object),
                Times.Exactly(2));
        }
    }
}