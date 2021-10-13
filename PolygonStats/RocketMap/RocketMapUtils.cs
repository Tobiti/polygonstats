using POGOProtos.Rpc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PolygonStats.RocketMap
{
    class RocketMapUtils
    {
        public class QuestText
        {
            public string prototext { get; set; }
            public string text { get; set; }
        }
        public class PokemonName
        {
            public string name { get; set; }
        }
        public class ItemText
        {
            public string protoname { get; set; }
            public string name { get; set; }
        }

        private static RocketMapUtils _shared;
        public static RocketMapUtils shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new RocketMapUtils();
                }
                return _shared;
            }
        }

        private Dictionary<String, QuestText> questTextDictionary;
        private Dictionary<String, String> pokemonTypesDictionary;
        private Dictionary<String, PokemonName> pokemonNamesDictionary;
        private Dictionary<String, ItemText> itemTextDictionary;
        private Dictionary<String, String> questTemplateDictionary;

        public RocketMapUtils()
        {
            questTextDictionary = GetJsonFile<Dictionary<String, QuestText>>("types");
            pokemonTypesDictionary = GetJsonFile<Dictionary<String, String>>("pokemonTypes");
            pokemonNamesDictionary = GetJsonFile<Dictionary<String, PokemonName>>("pokemon");
            itemTextDictionary = GetJsonFile<Dictionary<String, ItemText>>("items");
            questTemplateDictionary = GetJsonFile<Dictionary<String, String>>("quest_templates");
        }

        public T GetJsonFile<T>(String file) {
            try
            {
                using (StreamReader reader = new StreamReader($"locale/{CultureInfo.InstalledUICulture.TwoLetterISOLanguageName}/{file}.json"))
                {
                    return JsonSerializer.Deserialize<T>(reader.ReadToEnd());
                }
            } catch {
                using (StreamReader reader = new StreamReader($"locale/en/{file}.json"))
                {
                    return JsonSerializer.Deserialize<T>(reader.ReadToEnd());
                }
            }
        }

        public String GetQuestTypeText(QuestType quest_type)
        {
            String id = Convert.ToString((int)quest_type);
            if (questTextDictionary.ContainsKey(id))
            {
                return questTextDictionary[id].text;
            }
            return "Unknown quest type placeholder: {0}";
        }

        public String GetPokemonType(int pokemonType)
        {
            String id = Convert.ToString((int)pokemonType);
            if (pokemonTypesDictionary.ContainsKey(id))
            {
                return pokemonTypesDictionary[id];
            }
            return pokemonTypesDictionary["0"];
        }

        public String GetPokemonName(int pokemonId)
        {
            String id = Convert.ToString((int)pokemonId);
            if (pokemonNamesDictionary.ContainsKey(id))
            {
                return pokemonNamesDictionary[id].name;
            }
            return pokemonNamesDictionary["0"].name;
        }

        public String GetItemName(int itemId)
        {
            String id = Convert.ToString((int)itemId);
            if (itemTextDictionary.ContainsKey(id))
            {
                return itemTextDictionary[id].name;
            }
            return itemTextDictionary["0"].name;
        }

        public String GetQuestTemplateText(String templateId)
        {
            if (questTemplateDictionary.ContainsKey(templateId))
            {
                return questTemplateDictionary[templateId];
            }
            return null;
        }
        
    }
}
