using ConsoleClient.Domain;
using Slalom.Stacks.Communication;
using Slalom.Stacks.Serialization;

namespace ConsoleClient.Commands.AddItem
{
    public class AddItemCommand : Command<ItemAddedEvent>
    {
        public string Text { get; private set; }

        [Secure]
        public string SecureProperty { get; set; }

        [Ignore]
        public string IgnoredProperty { get; set; }

        public AddItemCommand(string text)
        {
            this.Text = text;
        }
    }
}