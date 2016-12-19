using System.Linq;
using System.Threading.Tasks;
using ConsoleClient.Domain;
using Slalom.Stacks.Communication;
using Slalom.Stacks.Domain;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Search;

namespace ConsoleClient.Search
{
    public class ItemSearchIndexer : SearchIndexer<ItemSearchResult>, IHandleEvent<ItemAddedEvent>
    {
        public ItemSearchIndexer(ISearchContext context)
            : base(context)
        {
        }

        public IDomainFacade Domain { get; set; }

        public override async Task RebuildIndexAsync()
        {
            await this.ClearAsync();

            var index = 0;
            var size = 1000;

            var set = this.Domain.OpenQuery<Item>();

            var working = set.Take(size).ToList();
            while (working.Any())
            {
                await this.AddAsync(working.Select(e => new ItemSearchResult()).ToArray());
                working = set.Skip(++index * size).Take(size).ToList();
            }
        }

        public Task Handle(ItemAddedEvent instance, ExecutionContext context)
        {
            return this.AddAsync(new[] { new ItemSearchResult
            {
                Text = instance.Item.Text
            }});
        }
    }
}