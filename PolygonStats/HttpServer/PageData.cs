using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            "  </body>" +
            "</html>";

        public static string getData(bool isAdmin)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string accName in StatManager.sharedInstance.getAllEntries().Keys)
            {
                sb.AppendLine(getTableEntry(accName, StatManager.sharedInstance.getAllEntries()[accName], isAdmin));
            }
            if (StatManager.sharedInstance.getAllEntries().Keys.Count > 1)
            {
                sb.AppendLine(getAverageTableEntry());
            }
            return String.Format(data, sb.ToString());
        }

        public static string getTableEntry(string accName, Stats stat, bool isAdmin)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<tr>");
            sb.Append("<td style=\"padding: 0px 10px 0px 10px;\">");
            sb.Append(getName(isAdmin, stat));
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.caughtPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.fleetPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.shinyPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append(stat.spinnedPokestops.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.getXpPerHour().ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.getXpPerDay().ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.xpTotal.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.getStardustPerHour().ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.getStardustPerDay().ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.stardustTotal.ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.getCaughtPokemonPerDay().ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append(stat.getSpinnedPokestopsPerDay().ToString());
            sb.Append("</td>");
            if (isAdmin)
            {
                sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">>");
                sb.Append($"<form method=\"post\" action=\"remove-{stat.accountName}\"><input type=\"submit\" value=\"Remove\"> </form>");
                sb.Append("</td>");
            }
            sb.Append("</tr>");

            return sb.ToString();
        }

        public static string getAverageTableEntry()
        {
            Dictionary<string, Stats> dic = StatManager.sharedInstance.getAllEntries();

            StringBuilder sb = new StringBuilder();
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
            sb.Append((dic.Values.Sum(s => s.caughtPokemon) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.fleetPokemon) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.shinyPokemon) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center\">");
            sb.Append((dic.Values.Sum(s => s.spinnedPokestops) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.getXpPerHour()) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.getXpPerDay()) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.xpTotal) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.getStardustPerHour()) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.getStardustPerDay()) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.stardustTotal) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.getCaughtPokemonPerDay()) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("<td style=\"text-align:center; padding: 0px 5px 0px 5px;\">");
            sb.Append((dic.Values.Sum(s => s.getSpinnedPokestopsPerDay()) / dic.Values.Count).ToString());
            sb.Append("</td>");
            sb.Append("</tr>");

            return sb.ToString();
        }

        private static string getName(bool isAdmin, Stats stat)
        {
            if(isAdmin)
            {
                return stat.accountName;
            }
            return stat.accountName.Substring(0, 5) + "XXXXX";
        }
    }
}
