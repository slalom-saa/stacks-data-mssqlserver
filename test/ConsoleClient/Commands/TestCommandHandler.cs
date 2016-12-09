using System.Threading.Tasks;
using ConsoleClient.Domain;
using Slalom.Stacks.Communication;

namespace ConsoleClient.Commands
{
    public class TestCommandHandler : CommandHandler<TestCommand, TestEvent>
    {
        public override Task<TestEvent> Handle(TestCommand command)
        {
            return Task.FromResult(new TestEvent());
        }
    }
}