using Microsoft.EntityFrameworkCore;
using POGOProtos.Rpc;
using System;
using System.Linq;
using Serilog;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Google.Protobuf.Collections;

namespace PolygonStats.RocketMap
{
    public enum SeenType
    {
        wild,
        encounter,
        nearby_stop,
        nearby_cell,
        lure_wild,
        lure_encounter
    }

    class RocketMapManager
    {
        private static RocketMapManager _shared;
        public static RocketMapManager shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new RocketMapManager();
                }
                return _shared;
            }
        }

        public void AddEncounter(EncounterOutProto encounter)
        {
            using (var context = new RocketMapContext())
            {
                DateTime disappearTime = DateTime.UtcNow;
                if (encounter.Pokemon.TimeTillHiddenMs > 0)
                {
                    disappearTime = disappearTime.AddMilliseconds(encounter.Pokemon.TimeTillHiddenMs);
                }
                else
                {
                    disappearTime = disappearTime.AddMinutes(20);
                }

                String query =  "INSERT INTO pokemon (encounter_id, spawnpoint_id, pokemon_id, latitude, longitude, disappear_time, " +
                                "individual_attack, individual_defense, individual_stamina, move_1, move_2, cp, cp_multiplier, " +
                                "weight, height, gender, catch_prob_1, catch_prob_2, catch_prob_3, rating_attack, rating_defense, " +
                                "weather_boosted_condition, last_modified, costume, form, seen_type) " +
                                "VALUES ({0}, {1}, {2}, {3}, {4}, \"{5}\", {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, " +
                                "{20}, {21}, \"{22}\", {23}, {24}, \"{25}\") " +
                                "ON DUPLICATE KEY UPDATE last_modified=VALUES(last_modified), disappear_time=VALUES(disappear_time), " +
                                "spawnpoint_id=VALUES(spawnpoint_id), pokemon_id=VALUES(pokemon_id), latitude=VALUES(latitude), " +
                                "longitude=VALUES(longitude), gender=VALUES(gender), costume=VALUES(costume), form=VALUES(form), " +
                                "weather_boosted_condition=VALUES(weather_boosted_condition), fort_id=NULL, cell_id=NULL, " +
                                "seen_type=IF(seen_type='encounter','encounter',VALUES(seen_type))";

                query = String.Format(query, encounter.Pokemon.EncounterId, Convert.ToInt64(encounter.Pokemon.SpawnPointId, 16), (int)encounter.Pokemon.Pokemon.PokemonId, encounter.Pokemon.Latitude, encounter.Pokemon.Longitude,
                    disappearTime.ToString("yyyy-MM-dd HH:mm:ss"), encounter.Pokemon.Pokemon.IndividualAttack, encounter.Pokemon.Pokemon.IndividualDefense, encounter.Pokemon.Pokemon.IndividualStamina, 
                    (int)encounter.Pokemon.Pokemon.Move1, (int)encounter.Pokemon.Pokemon.Move2, encounter.Pokemon.Pokemon.Cp, encounter.Pokemon.Pokemon.CpMultiplier, encounter.Pokemon.Pokemon.WeightKg,
                    encounter.Pokemon.Pokemon.HeightM, (int)encounter.Pokemon.Pokemon.PokemonDisplay.Gender, encounter.CaptureProbability.CaptureProbability[0], encounter.CaptureProbability.CaptureProbability[1],
                    encounter.CaptureProbability.CaptureProbability[2], "\"\"", "\"\"", (int)encounter.Pokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition, UnixTimeStampToDateTime(encounter.Pokemon.LastModifiedMs).ToString("yyyy-MM-dd HH:mm:ss"),
                    (int)encounter.Pokemon.Pokemon.PokemonDisplay.Costume, (int)encounter.Pokemon.Pokemon.PokemonDisplay.Form, SeenType.wild.ToString("g"));
                try
                {
                    context.Database.ExecuteSqlRaw(query);
                }
                catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.StackTrace);
                    Log.Information($"Object: {JsonSerializer.Serialize(encounter)} \n Query: {query}");

                }
            }
        }

        public void AddForts(List<PokemonFortProto> forts)
        {
            using (var context = new RocketMapContext())
            {
                foreach (PokemonFortProto fort in forts)
                {
                    if (fort.FortType == FortType.Checkpoint)
                    {
                        String query =  "INSERT INTO pokestop (pokestop_id, enabled, latitude, longitude, last_modified, lure_expiration, " +
                                        "last_updated, image, active_fort_modifier, incident_start, incident_expiration, incident_grunt_type, " +
                                        "is_ar_scan_eligible) " +
                                        "VALUES (\"{0}\", {1}, {2}, {3}, \"{4}\"," +
                                        " date_add(\"{5}\",interval COALESCE((select event_lure_duration from trs_event where now() between event_start and event_end order by event_start desc limit 1), 30) minute)," +
                                        " \"{6}\", \"{7}\", {8}, {9}, {10}, {11}, {12}) " +
                                        "ON DUPLICATE KEY UPDATE last_updated=VALUES(last_updated), lure_expiration=VALUES(lure_expiration), " +
                                        "last_modified=VALUES(last_modified), latitude=VALUES(latitude), longitude=VALUES(longitude), " +
                                        "active_fort_modifier=VALUES(active_fort_modifier), incident_start=VALUES(incident_start), " +
                                        "incident_expiration=VALUES(incident_expiration), incident_grunt_type=VALUES(incident_grunt_type), " +
                                        "is_ar_scan_eligible=VALUES(is_ar_scan_eligible)," +
                                        "image=IF(VALUES(image) IS NOT NULL AND VALUES(image) <> '', VALUES(image), image)";
                        try
                        {
                            query = String.Format(query, fort.FortId, fort.Enabled, fort.Latitude, fort.Longitude, ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs)), 
                                                        ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs)), 
                                                        ToMySQLDateTime(DateTime.UtcNow), fort.ImageUrl, (fort.ActiveFortModifier.Count > 0 ? (int)fort.ActiveFortModifier[0] : 0),
                                                        (fort.PokestopDisplays.Count <= 0 ? "NULL" : $"\"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.PokestopDisplays[0].IncidentStartMs))}\""),
                                                        (fort.PokestopDisplays.Count <= 0 ? "NULL" : $"\"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.PokestopDisplays[0].IncidentExpirationMs))}\""),
                                                        (fort.PokestopDisplays.Count <= 0 ? "NULL" : (int)fort.PokestopDisplays[0].CharacterDisplay.Character),
                                                        fort.IsArScanEligible);

                            context.Database.ExecuteSqlRaw(query);
                        }
                        catch (Exception e)
                        {
                            Log.Information(e.Message);
                            Log.Information(e.StackTrace);
                            Log.Information($"Object: {JsonSerializer.Serialize(fort)} \n Query: {query}");
                        }
                    } else
                    {
                        AddGym(fort, context);
                    }
                }
            }
        }

        public void UpdateFortInformations(FortDetailsOutProto fort)
        {

            using (var context = new RocketMapContext())
            {
                String query = "INSERT INTO pokestop (pokestop_id, enabled, latitude, longitude, last_modified, " +
                                "last_updated, name, image) " +
                                "VALUES (\"{0}\", {1}, {2}, {3}, \"{4}\", \"{5}\", {6}, {7}) " +
                                "ON DUPLICATE KEY UPDATE last_updated=VALUES(last_updated), lure_expiration=VALUES(lure_expiration), " +
                                "latitude=VALUES(latitude), longitude=VALUES(longitude), name=VALUES(name)," +
                                "image = IF(VALUES(image) IS NOT NULL AND VALUES(image) <> '', VALUES(image), image)";
                try
                {
                    query = String.Format(query, fort.Id, 1, fort.Latitude, fort.Longitude, ToMySQLDateTime(DateTime.UtcNow),
                                                ToMySQLDateTime(DateTime.UtcNow),
                                                fort.Name != null ? $"\"{MySQLEscape(fort.Name)}\"" : "NULL",
                                                fort.ImageUrl.Count > 0 ? $"\"{MySQLEscape(fort.ImageUrl[0])}\"" : "NULL");

                    context.Database.ExecuteSqlRaw(query);
                }
                catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.StackTrace);
                    Log.Information($"Object: {JsonSerializer.Serialize(fort)} \n Query: {query}");
                }
            }
        }

        public void AddQuest(FortSearchOutProto fort)
        {

            using (var context = new RocketMapContext())
            {
                if (fort.FortId == null || fort.ChallengeQuest == null || fort.ChallengeQuest.Quest.QuestRewards == null || fort.ChallengeQuest.Quest.QuestRewards.Count <= 0)
                {
                    return;
                }
                JsonSerializerOptions jsonSettings = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                String query =  "INSERT INTO trs_quest (GUID, quest_type, quest_timestamp, quest_stardust, quest_pokemon_id, " +
                                "quest_pokemon_form_id, quest_pokemon_costume_id, " +
                                "quest_reward_type, quest_item_id, quest_item_amount, quest_target, quest_condition, quest_reward, " +
                                "quest_task, quest_template) VALUES (\"{0}\", {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, \'{11}\', \'{12}\', \"{13}\", \"{14}\")" +
                                "ON DUPLICATE KEY UPDATE quest_type=VALUES(quest_type), quest_timestamp=VALUES(quest_timestamp), " +
                                "quest_stardust=VALUES(quest_stardust), quest_pokemon_id=VALUES(quest_pokemon_id), " +
                                "quest_reward_type=VALUES(quest_reward_type), quest_item_id=VALUES(quest_item_id), " +
                                "quest_item_amount=VALUES(quest_item_amount), quest_target=VALUES(quest_target), " +
                                "quest_condition=VALUES(quest_condition), quest_reward=VALUES(quest_reward), " +
                                "quest_task=VALUES(quest_task), quest_template=VALUES(quest_template), " +
                                "quest_pokemon_form_id=VALUES(quest_pokemon_form_id), " +
                                "quest_pokemon_costume_id=VALUES(quest_pokemon_costume_id)";

                var quest = fort.ChallengeQuest.Quest;
                var reward = fort.ChallengeQuest.Quest.QuestRewards[0];

                int itemAmount = reward.Item != null ? (int)reward.Item.Amount : 0;
                int pokemonId = reward.PokemonEncounter != null ? (int)reward.PokemonEncounter.PokemonId : 0;

                if(reward.Type == QuestRewardProto.Types.Type.Candy)
                {
                    itemAmount = reward.Candy.Amount;
                    pokemonId = (int)reward.Candy.PokemonId;
                }
                if (reward.Type == QuestRewardProto.Types.Type.MegaResource)
                {
                    itemAmount = reward.MegaResource.Amount;
                    pokemonId = (int)reward.MegaResource.PokemonId;
                }

                int FormId = 0;
                int CostumeId = 0;
                if(reward.PokemonEncounter != null && reward.PokemonEncounter.PokemonDisplay != null)
                {
                    FormId = (int)reward.PokemonEncounter.PokemonDisplay.Form;
                    CostumeId = (int)reward.PokemonEncounter.PokemonDisplay.Costume;
                }

                try
                {
                    //TODO: Add task text
                    query = String.Format(query, fort.FortId,
                                                (int)quest.QuestType,
                                                DateTimeOffset.Now.ToUnixTimeSeconds(),
                                                reward.Stardust,
                                                pokemonId,
                                                FormId,
                                                CostumeId,
                                                (int)reward.Type,
                                                reward.Item != null ? (int)reward.Item.Item : "0",
                                                itemAmount,
                                                quest.Goal.Target,
                                                JsonSerializer.Serialize(quest.Goal.Condition, jsonSettings),
                                                JsonSerializer.Serialize(quest.QuestRewards, jsonSettings),
                                                GetQuestTaskText(quest.QuestType, quest.Goal.Condition[0], quest.Goal.Target, quest.TemplateId), // Task text
                                                quest.TemplateId).Replace("{", "{{").Replace("}", "}}");

                    context.Database.ExecuteSqlRaw(query);
                }
                catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.StackTrace);
                    Log.Information($"Object: {JsonSerializer.Serialize(fort)} \n Query: {query}");
                }
            }
        }

        public void AddGym(PokemonFortProto gym, RocketMapContext context)
        {
            String queryGym = "INSERT INTO gym (gym_id, team_id, guard_pokemon_id, slots_available, enabled, latitude, longitude, " +
                                "total_cp, is_in_battle, last_modified, last_scanned, is_ex_raid_eligible, is_ar_scan_eligible) " +
                                "VALUES (\"{0}\", {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, \"{9}\", \"{10}\", {11}, {12}) " +
                                "ON DUPLICATE KEY UPDATE " +
                                "guard_pokemon_id=VALUES(guard_pokemon_id), team_id=VALUES(team_id), " +
                                "slots_available=VALUES(slots_available), last_scanned=VALUES(last_scanned), " +
                                "last_modified=VALUES(last_modified), latitude=VALUES(latitude), longitude=VALUES(longitude), " +
                                "is_ex_raid_eligible=VALUES(is_ex_raid_eligible), is_ar_scan_eligible=VALUES(is_ar_scan_eligible)";
            String queryGymDetails = "INSERT INTO gymdetails (gym_id, name, url, last_scanned) " +
                                        "VALUES (\"{0}\", {1}, \'{2}\', \"{3}\") " +
                                        "ON DUPLICATE KEY UPDATE last_scanned=VALUES(last_scanned), " +
                                        "url=IF(VALUES(url) IS NOT NULL AND VALUES(url) <> '', VALUES(url), url)";

            queryGym = String.Format(queryGym,
                                            gym.FortId,
                                            (int)gym.Team,
                                            (int)gym.GuardPokemonId,
                                            gym.GymDisplay.SlotsAvailable,
                                            1,
                                            gym.Latitude,
                                            gym.Longitude,
                                            gym.GymDisplay.TotalGymCp,
                                            gym.IsInBattle,
                                            ToMySQLDateTime(UnixTimeStampToDateTime(gym.LastModifiedMs)),
                                            ToMySQLDateTime(DateTime.Now),
                                            gym.IsExRaidEligible,
                                            gym.IsArScanEligible);

            queryGymDetails = String.Format(queryGymDetails,
                                            gym.FortId,
                                            "\"unknown\"",
                                            MySQLEscape(gym.ImageUrl),
                                            ToMySQLDateTime(DateTime.Now));

            try
            {
                context.Database.ExecuteSqlRaw(queryGym);
                context.Database.ExecuteSqlRaw(queryGymDetails);
            }
            catch (Exception e)
            {
                Log.Information(e.Message);
                Log.Information(e.StackTrace);
                Log.Information($"Object: {JsonSerializer.Serialize(gym)} \n\n Gym Query: {queryGym} \n\n Gym Details Query: {queryGymDetails}");
            }

            if(gym.RaidInfo != null)
            {
                AddRaid(gym, context);
            }
        }

        public void UpdateGymDetails(GymGetInfoOutProto gym)
        {
            using (var context = new RocketMapContext())
            {
                if (gym.GymStatusAndDefenders == null || gym.GymStatusAndDefenders.PokemonFortProto == null)
                {
                    return;
                }

                List<String> parameters = new List<String>();
                if (gym.Name != null && gym.Name.Length > 0)
                {
                    parameters.Add($"name=\'{MySQLEscape(gym.Name)}\'");
                }
                if (gym.Name != null && gym.Name.Length > 0)
                {
                    parameters.Add($"description=\'{MySQLEscape(gym.Description)}\'");
                }
                if (gym.Name != null && gym.Name.Length > 0)
                {
                    parameters.Add($"url=\"{MySQLEscape(gym.Url)}\"");
                }

                String updateQUery = $"UPDATE gymdetails SET {String.Join(",", parameters.ToArray())} WHERE gym_id = \"{gym.GymStatusAndDefenders.PokemonFortProto.FortId}\"";

                try
                {
                    context.Database.ExecuteSqlRaw(updateQUery);
                }
                catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.StackTrace);
                    Log.Information($"Object: {JsonSerializer.Serialize(gym)} \n\n Gym Query: {updateQUery}");
                }
            }
        }

        public void AddRaid(PokemonFortProto gym, RocketMapContext context)
        {
            String query =  "INSERT INTO raid (gym_id, level, spawn, start, end, pokemon_id, cp, move_1, move_2, last_scanned, form, " +
                            "is_exclusive, gender, costume, evolution) " +
                            "VALUES (\"{0}\", {1}, \"{2}\", \"{3}\", \"{4}\", {5}, {6}, {7}, {8}, \"{9}\", {10}, {11}, {12}, {13}, {14}) " +
                            "ON DUPLICATE KEY UPDATE level=VALUES(level), spawn=VALUES(spawn), start=VALUES(start), " +
                            "end=VALUES(end), pokemon_id=VALUES(pokemon_id), cp=VALUES(cp), move_1=VALUES(move_1), " +
                            "move_2=VALUES(move_2), last_scanned=VALUES(last_scanned), is_exclusive=VALUES(is_exclusive), " +
                            "form=VALUES(form), gender=VALUES(gender), costume=VALUES(costume), evolution=VALUES(evolution)";

            List<Object> parameters = new List<Object>();
            parameters.Add(gym.FortId);
            parameters.Add((int)gym.RaidInfo.RaidLevel);
            parameters.Add(ToMySQLDateTime(UnixTimeStampToDateTime(gym.RaidInfo.RaidSpawnMs)));
            parameters.Add(ToMySQLDateTime(UnixTimeStampToDateTime(gym.RaidInfo.RaidBattleMs)));
            parameters.Add(ToMySQLDateTime(UnixTimeStampToDateTime(gym.RaidInfo.RaidEndMs)));

            if (gym.RaidInfo.RaidPokemon != null)
            {
                parameters.Add((int)gym.RaidInfo.RaidPokemon.PokemonId);
                parameters.Add(gym.RaidInfo.RaidPokemon.Cp);
                parameters.Add((int)gym.RaidInfo.RaidPokemon.Move1);
                parameters.Add((int)gym.RaidInfo.RaidPokemon.Move2);
                parameters.Add(ToMySQLDateTime(DateTime.Now));
                parameters.Add((int)gym.RaidInfo.RaidPokemon.PokemonDisplay.Form);
                parameters.Add(gym.RaidInfo.IsExclusive);
                parameters.Add((int)gym.RaidInfo.RaidPokemon.PokemonDisplay.Gender);
                parameters.Add((int)gym.RaidInfo.RaidPokemon.PokemonDisplay.Costume);
                parameters.Add((int)gym.RaidInfo.RaidPokemon.PokemonDisplay.CurrentTempEvolution);
            } else
            {
                parameters.Add("NULL");
                parameters.Add(0);
                parameters.Add(1);
                parameters.Add(2);
                parameters.Add(ToMySQLDateTime(DateTime.Now));
                parameters.Add("NULL");
                parameters.Add(gym.RaidInfo.IsExclusive);
                parameters.Add("NULL");
                parameters.Add("NULL");
                parameters.Add(0);
            }

            try
            {
                query = String.Format(query, parameters.ToArray());

                context.Database.ExecuteSqlRaw(query);
            }
            catch (Exception e)
            {
                Log.Information(e.Message);
                Log.Information(e.StackTrace);
                Log.Information($"Object: {JsonSerializer.Serialize(gym)} \n\n Raid Query: {query} \n\n Params: {JsonSerializer.Serialize(parameters)}");
            }
        }

        public String ToMySQLDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }
        private static string MySQLEscape(string str)
        {
            return Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%]",
                delegate (Match match)
                {
                    string v = match.Value;
                    switch (v)
                    {
                        case "\x00":            // ASCII NUL (0x00) character
                    return "\\0";
                        case "\b":              // BACKSPACE character
                    return "\\b";
                        case "\n":              // NEWLINE (linefeed) character
                    return "\\n";
                        case "\r":              // CARRIAGE RETURN character
                    return "\\r";
                        case "\t":              // TAB
                    return "\\t";
                        case "\u001A":          // Ctrl-Z
                    return "\\Z";
                        default:
                            return "\\" + v;
                    }
                });
        }

        private String GetQuestTaskText(QuestType type, QuestConditionProto condition, int target, String templateId)
        {
            if (RocketMapUtils.shared.GetQuestTemplateText(templateId) != null)
            {
                return RocketMapUtils.shared.GetQuestTemplateText(templateId);
            }
            List<object> parameters = new List<object>();
            parameters.Add(Convert.ToString(target));
            String text = RocketMapUtils.shared.GetQuestTypeText(type);

            switch (type)
            {
                case QuestType.QuestCatchPokemon:

                    break;
                case QuestType.QuestSpinPokestop:
                    if(condition.Type == QuestConditionProto.Types.ConditionType.WithUniquePokestop){
                        text = "Spin {0} Pokestops you haven't visited before.";
                    } else
                    {
                        text = "Spin {0} Pokestops or Gyms.";
                    }
                    break;
                case QuestType.QuestCompleteGymBattle:
                    if (condition.Type == QuestConditionProto.Types.ConditionType.WithWinGymBattleStatus)
                    {
                        text = "Win {0} Gym Battles.";
                    } else
                    {
                        if (condition.Type == QuestConditionProto.Types.ConditionType.WithSuperEffectiveCharge)
                        {
                            text = "Use a supereffective Charged Attack in {0} Gym battles.";
                        }
                    }
                    break;
                case QuestType.QuestCompleteRaidBattle:
                    if (condition != null)
                    {
                        text = "Win {0} Raids.";
                        switch (condition.WithRaidLevel.RaidLevel[0])
                        {
                            case RaidLevel._2:
                                text = "Win a level 2 or higher raid.";
                                break;
                            case RaidLevel._3:
                                text = "Win a level 3 or higher raid.";
                                break;
                            case RaidLevel.Mega:
                                text = "Win a Mega raid.";
                                break;
                        }
                    }
                    break;
                case QuestType.QuestUseBerryInEncounter:
                    text = "Use {0} {1}Berries to help catch Pokemon.";
                    String berrie = "";
                    if (condition.WithItem != null)
                    {
                        berrie = RocketMapUtils.shared.GetItemName((int)condition.WithItem.Item);
                    }
                    parameters.Add(berrie);
                    break;
                case QuestType.QuestLandThrow:
                    parameters.Add("");
                    parameters.Add("");
                    parameters.Add("");

                    text = "Make {0} {1}{2}Throws{3}.";

                    if(condition.WithThrowType != null)
                    {
                        parameters[1] = condition.WithThrowType.ThrowType.ToString("G");
                    }
                    if (condition.Type == QuestConditionProto.Types.ConditionType.WithCurveBall)
                    {
                        parameters[2] = "Curveball ";
                    }
                    if (condition.Type == QuestConditionProto.Types.ConditionType.WithThrowTypeInARow)
                    {
                        parameters[3] = " in a row";
                    }

                    break;
            }

            if (target == 1)
            {
                text = text.Replace(" Eggs", "n Egg");
                text = text.Replace(" Raids", " Raid");
                text = text.Replace(" Battles", " Battle");
                text = text.Replace(" candies", " candy");
                text = text.Replace(" gifts", " gift");
                text = text.Replace(" Pokestops", " Pokestop");
                text = text.Replace(" {0} snapshots", " a snapshot");
                text = text.Replace("Make {0} {1}{2}Throws", "Make a {1}{2}Throw");
                text = text.Replace(" {0} times", "");
                text = text.Replace("{0} hearts", "a heart");
                parameters[0] = "a";
            }

            text = String.Format(text, parameters.ToArray());
            return text;
        }
    }
}
