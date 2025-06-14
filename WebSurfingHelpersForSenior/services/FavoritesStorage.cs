using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace WebSurfingHelpersForSenior.services
{
    internal class FavoritesStorage
    {
        public static List<FavoriteItem> Load()
        {
            var json = Properties.Settings.Default.FavoritesJson;
            if (string.IsNullOrEmpty(json)) return new List<FavoriteItem>();
            return JsonConvert.DeserializeObject<List<FavoriteItem>>(json) ?? new List<FavoriteItem>();
        }

        public static void Save(List<FavoriteItem> list)
        {
            var json = JsonConvert.SerializeObject(list, Newtonsoft.Json.Formatting.Indented);
            Properties.Settings.Default.FavoritesJson = json;
            Properties.Settings.Default.Save();
        }
    }
}
