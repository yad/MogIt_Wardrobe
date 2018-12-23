using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace MogIt_Wardrobe
{
    public static class Program
    {
        private static readonly Dictionary<string, string[]> itemSetsDatabase = new Dictionary<string, string[]>()
        {
            { 
                "Cloth", 
                new [] 
                {
                    "https://fr.wowhead.com/transmog-sets/max-req-level:90/type:1",
                    "https://fr.wowhead.com/transmog-sets/min-req-level:90/type:1"
                } 
            },            
            { 
                "Leather", 
                new [] 
                {
                    "https://fr.wowhead.com/transmog-sets/max-req-level:90/type:2",
                    "https://fr.wowhead.com/transmog-sets/min-req-level:90/type:2"
                } 
            },
            { 
                "Mail", 
                new [] 
                {
                    "https://fr.wowhead.com/transmog-sets/max-req-level:90/type:3",
                    "https://fr.wowhead.com/transmog-sets/min-req-level:90/type:3"
                } 
            },
            { 
                "Plate", 
                new [] 
                {
                    "https://fr.wowhead.com/transmog-sets/max-req-level:90/type:4",
                    "https://fr.wowhead.com/transmog-sets/min-req-level:90/type:4"
                } 
            }
        };

        public static void Main(string[] args)
        {
            foreach (var itemSetsTypeDatabase in itemSetsDatabase)
            {
                string fileName = $"MogIt_Wardrobe\\{itemSetsTypeDatabase.Key}.lua";
                File.WriteAllText(fileName, $"local a,t=...\r\nlocal s=t.Add{itemSetsTypeDatabase.Key}\r\n");

                List<ItemSet> itemSetsForType = GetItemSetsForType(itemSetsTypeDatabase);
                var groupedByFamilyItemSets = itemSetsForType.OrderBy(i => i.Name).GroupBy(i => i.ShortName);
                var sortedItemSets = new List<ItemSet>();
                foreach(var group in groupedByFamilyItemSets.OrderBy(g => g.Min(i => i.Id)))
                {
                    sortedItemSets.AddRange(group);
                }

                File.AppendAllLines(fileName, sortedItemSets.Select(i => i.ToString()).Distinct());
            }            
        }

        private static List<ItemSet> GetItemSetsForType(KeyValuePair<string, string[]> itemSetsTypeDatabase)
        {
            List<ItemSet> itemSets =  new List<ItemSet>();
            foreach (var url in itemSetsTypeDatabase.Value)
            {
                var task = GetItemSet(url);                
                var items = task.GetAwaiter().GetResult();
                itemSets.AddRange(items);
            }

            return itemSets;
        }

        private static async Task<IEnumerable<ItemSet>> GetItemSet(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
                var result = await client.GetStringAsync(url);
                return GetItemSetCollection(result);
            }
        }

        private static IEnumerable<ItemSet> GetItemSetCollection(string htmlContent)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlContent);

            var scriptElement = htmlDocument.DocumentNode.Descendants().First(e => e.Attributes.Any(a => a.Name == "type" && e.Attributes["type"].Value == "text/javascript")).InnerText;

            var jsonItemSet = scriptElement
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .First(e => e.StartsWith("var transmogSets = ")).Replace("var transmogSets = ", "");
            
            var itemSet = JsonConvert.DeserializeObject<List<ItemSet>>(jsonItemSet);
            if (itemSet.Count == 500)
            {
                throw new ApplicationException("L'une des urls retourne 500 résultats.");
            }

            return itemSet;
        }
    }

    public class ItemSet
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public string ShortName { get { return Name.IndexOf("(") == -1 ? Name : Name.Substring(0, Name.IndexOf("(")).Trim(); } }

        [JsonProperty("pieces")]
        public int[] Pieces { get; set; }

        [JsonProperty("reqclass")]
        public int? ReqClass { get; set; }

        public override string ToString()
        {
            return $"s({Id},\"{Name.Substring(1)}\",{{{string.Join(",", Pieces)}}},{(ReqClass == null ? "nil" : ReqClass.ToString())})";
        }
    }

}
