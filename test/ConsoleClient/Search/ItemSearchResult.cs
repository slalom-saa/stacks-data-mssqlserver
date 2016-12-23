using System;
using Slalom.Stacks.Search;

namespace ConsoleClient.Search
{
    public class ItemSearchResult : ISearchResult
    {
        public int Id { get; set; }

        public string Text { get; set; }
    }
}