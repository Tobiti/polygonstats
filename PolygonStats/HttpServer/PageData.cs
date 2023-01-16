using PolygonStats.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PolygonStats.HttpServer
{
    class PageData
    {
        private static readonly string data =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <meta http-equiv=\"refresh\" content=\"10\">" +
            "    <title>Polygon Stats</title>" +
            "  </head>" +
            "  <body>" +
            "    <table style=\"border-collapse: collapse;\">" +
            "       <tr>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Account Name" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Caught Pokemon" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Escaped Pokemon" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Shiny Pokemon" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Spinned Pokestops" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               XP/h" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               XP/Day" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               XP Total" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Stardust/h" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Stardust/Day" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Stardust Total" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Caught Pokemon / Day" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "               Spinned Pokestops / Day" +
            "           </th>" +
            "           <th style=\"text-align:center; padding: 0px 5px 0px 5px;\">" +
            "              " +
            "           </th>" +
            "       </tr>" +
            "{0}" +
            "    </table>" +
            "  <br />" +
            "  <br />" +
            "  Support my work by clicking <a href=\"https://paypal.me/pools/c/8nDB1mCCQz\">here.</a>" +
            "  </body>" +
            "</html>";

        public static string getData(bool isAdmin)
        {
            StringBuilder sb = new();
            foreach (string accName in StatManager.sharedInstance.getAllEntries().Keys)
            {
                sb.AppendLine(getTableEntry(accName, StatManager.sharedInstance.getAllEntries()[accName], isAdmin));
            }
            if (StatManager.sharedInstance.getAllEntries().Keys.Count > 1)
            {
                sb.AppendLine(getAverageTableEntry());
            }
            return string.Format(data, sb.ToString());
        }

        public static string getTableEntry(string accName, Stats stat, bool isAdmin)
        {
            StringBuilder sb = new();
            sb.Append("<tr>");
            sb.Append("<td style=\"padding: 0px 10px 0px 10px;\">");
            sb.Append(getName(isAdmin, stat));
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.CaughtPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.FleetPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.ShinyPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.SpinnedPokestops.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.XpPerHour.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.XpPerDay.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.XpTotal.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.StardustPerHour.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.StardustPerDay.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.StardustTotal.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.CaughtPokemonPerDay.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.SpinnedPokestopsPerDay.ToString());
            sb.Append("</td>");
            if (isAdmin)
            {
                sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
                sb.Append($"<form method=\"post\" action=\"remove-{stat.AccountName}\"><input type=\"submit\" value=\"Remove\"> </form>");
                sb.Append("</td>");
            }
            sb.Append("</tr>");

            return sb.ToString();
        }

        public static string getAverageTableEntry()
        {
            Dictionary<string, Stats> dic = StatManager.sharedInstance.getAllEntries();

            StringBuilder sb = new();
            // Add a black line
            sb.AppendLine("<tr style=\"border-bottom: 1px solid black\">" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/>" +
                "<td style=\"border-bottom: 1px solid black\"/></tr>");

            sb.Append("<tr>");
            sb.Append("<td style=\"padding: 0px 10px 0px 10px;\">");
            sb.Append("Average Stats");
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.CaughtPokemon) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.FleetPokemon) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.ShinyPokemon) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.SpinnedPokestops) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.XpPerHour) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.XpPerDay) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.XpTotal) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.StardustPerHour) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.StardustPerDay) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.StardustTotal) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.CaughtPokemonPerDay) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.SpinnedPokestopsPerDay) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("</tr>");

            return sb.ToString();
        }

        private static string getName(bool isAdmin, Stats stat)
        {
            if (isAdmin || ConfigurationManager.Shared.Config.Http.ShowAccountNames)
            {
                return stat.AccountName;
            }
            return $"{stat.AccountName.Substring(0, 2)}XXXXXX{stat.AccountName.Substring(stat.AccountName.Length - 2, 2)}";
        }
    }
}
