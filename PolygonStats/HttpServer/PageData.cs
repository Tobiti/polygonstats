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
            "    <title>Polygon Stats</title>" +
            "  </head>" +
            "  <body>" +
            "    <table>" +
            "       <tr>" +
            "           <th>" +
            "               Account Name" +
            "           </th>" +
            "           <th>" +
            "               Caught Pokemon" +
            "           </th>" +
            "           <th>" +
            "               Escaped Pokemon" +
            "           </th>" +
            "           <th>" +
            "               Shiny Pokemon" +
            "           </th>" +
            "           <th>" +
            "               Spinned Pokestops" +
            "           </th>" +
            "           <th>" +
            "               XP Total" +
            "           </th>" +
            "           <th>" +
            "               XP/h" +
            "           </th>" +
            "           <th>" +
            "               Stardust Total" +
            "           </th>" +
            "           <th>" +
            "               Stardust/h" +
            "           </th>" +
            "           <th>" +
            "              " +
            "           </th>" +
            "       </tr>" +
            "{0}" +
            "    </table>" +
            "  </body>" +
            "</html>";

        public static string getData()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string accName in StatManager.sharedInstance.getAllEntries().Keys)
            {
                sb.AppendLine(getTableEntry(accName, StatManager.sharedInstance.getAllEntries()[accName]));
            }
            return String.Format(data, sb.ToString());
        }

        public static string getTableEntry(string accName, Stats stat)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<tr>");
            sb.Append("<td>");
            sb.Append(stat.accountName.Substring(0,5).PadRight(5, 'X'));
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.caughtPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.fleetPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.shinyPokemon.ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.spinnedPokestops.ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.xpTotal.ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.getXpPerHour().ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.stardustTotal.ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append(stat.getStardustPerHour().ToString());
            sb.Append("</td>");
            sb.Append("<td>");
            sb.Append($"<form method=\"post\" action=\"remove-{stat.accountName}\"><input type=\"submit\" value=\"Remove\"> </form>");
            sb.Append("</td>");
            sb.Append("</tr>");

            return sb.ToString();
        }
    }
}
