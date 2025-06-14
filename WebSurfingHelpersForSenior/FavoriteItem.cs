using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSurfingHelpersForSenior
{
    internal class FavoriteItem
    {
        public string Name { get; set; }    // ex. "네이버", "학교 홈피"
        public string Url { get; set; }     // ex. "https://www.naver.com"

        public override string ToString() => $"{Name} - {Url}";
    }
}
