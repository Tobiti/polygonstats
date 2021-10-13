using Microsoft.EntityFrameworkCore;
using POGOProtos.Rpc;
using System;
using System.Linq;
using Serilog;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Google.Protobuf.Collections;
using Google.Common.Geometry;
using System.Data.SqlClient;
using System.Collections;
using MySqlConnector;

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

        public void AddEncounter(EncounterOutProto encounter, Payload payload)
        {
            using (var context = new RocketMapContext())
            {
                long spawnpointId = Convert.ToInt64(encounter.Pokemon.SpawnPointId, 16);
                Spawnpoint spawnpoint = context.Spawnpoints.FromSqlInterpolated($"SELECT spawnpoint, spawndef, calc_endminsec FROM trs_spawn WHERE spawnpoint={spawnpointId}").FirstOrDefault();
                DateTime disappearTime = getDespawnTime(spawnpoint, encounter.Pokemon.LastModifiedMs, encounter.Pokemon.TimeTillHiddenMs, payload.timestamp);

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

                query = String.Format(query, encounter.Pokemon.EncounterId, spawnpointId, (int)encounter.Pokemon.Pokemon.PokemonId, encounter.Pokemon.Latitude, encounter.Pokemon.Longitude,
                    ToMySQLDateTime(disappearTime), encounter.Pokemon.Pokemon.IndividualAttack, encounter.Pokemon.Pokemon.IndividualDefense, encounter.Pokemon.Pokemon.IndividualStamina, 
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

        private DateTime getDespawnTime(Spawnpoint spawnpoint, long lastModifiedMs, long tillDespawnMs, long timestamp)
        {
            var now = UnixTimeStampToDateTime(timestamp);

            if (spawnpoint != null && spawnpoint.calc_endminsec != null && spawnpoint.calc_endminsec.Length != 0)
            {
                var split = spawnpoint.calc_endminsec.Split(":");
                var despawnDateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, int.Parse(split[0]), int.Parse(split[1]));
                if (despawnDateTime < now)
                {
                    despawnDateTime = despawnDateTime.AddHours(1);
                }
                return despawnDateTime;
            }
            else
            {
                if (30 < tillDespawnMs && tillDespawnMs <= 90000)
                {
                    return now.AddMilliseconds(tillDespawnMs);
                }
                else
                {
                    return now.AddMinutes(10);
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
                                        " {5}," +
                                        " \"{6}\", \"{7}\", {8}, {9}, {10}, {11}, {12}) " +
                                        "ON DUPLICATE KEY UPDATE last_updated=VALUES(last_updated), lure_expiration=VALUES(lure_expiration), " +
                                        "last_modified=VALUES(last_modified), latitude=VALUES(latitude), longitude=VALUES(longitude), " +
                                        "active_fort_modifier=VALUES(active_fort_modifier), incident_start=VALUES(incident_start), " +
                                        "incident_expiration=VALUES(incident_expiration), incident_grunt_type=VALUES(incident_grunt_type), " +
                                        "is_ar_scan_eligible=VALUES(is_ar_scan_eligible)," +
                                        "image=IF(VALUES(image) IS NOT NULL AND VALUES(image) <> '', VALUES(image), image)";
                        try
                        {
                            var activeModifier = (fort.ActiveFortModifier.Count > 0 ? (int)fort.ActiveFortModifier[0] : 0);
                            query = String.Format(query, fort.FortId, fort.Enabled, fort.Latitude, fort.Longitude, ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs)),
                                                        activeModifier == 0 ? $"\"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs))}\"" : $"date_add(\"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs))}\",interval COALESCE((select event_lure_duration from trs_event where now() between event_start and event_end order by event_start desc limit 1), 30) minute)", 
                                                        ToMySQLDateTime(DateTime.UtcNow), fort.ImageUrl, activeModifier,
                                                        (fort.PokestopDisplays.Count <= 0 || isRocketLeader(fort.PokestopDisplays) ? "NULL" : $"\"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.PokestopDisplays[0].IncidentStartMs))}\""),
                                                        (fort.PokestopDisplays.Count <= 0 || isRocketLeader(fort.PokestopDisplays) ? "NULL" : $"\"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.PokestopDisplays[0].IncidentExpirationMs))}\""),
                                                        (fort.PokestopDisplays.Count <= 0 || isRocketLeader(fort.PokestopDisplays) ? "NULL" : (int)fort.PokestopDisplays[0].CharacterDisplay.Character),
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

        private bool isRocketLeader(RepeatedField<PokestopIncidentDisplayProto> rockets)
        {
            foreach (var rocket in rockets) {
                switch (rocket.CharacterDisplay.Character) {
                    case EnumWrapper.Types.InvasionCharacter.CharacterGiovanni:
                        return true;
                    case EnumWrapper.Types.InvasionCharacter.CharacterExecutiveArlo:
                        return true;
                    case EnumWrapper.Types.InvasionCharacter.CharacterExecutiveCliff:
                        return true;
                    case EnumWrapper.Types.InvasionCharacter.CharacterExecutiveSierra:
                        return true;
                }
            }
            return false;
        }

        public void AddSpawnpoints(GetMapObjectsOutProto mapProto)
        {
            if (mapProto.MapCell == null || mapProto.MapCell.Count == 0)
            {
                return;
            }
            var cells = mapProto.MapCell;
            using (var context = new RocketMapContext())
            {

                String spawnpointsQuery = "INSERT INTO trs_spawn(spawnpoint, latitude, longitude, earliest_unseen, " +
                                            "last_scanned, spawndef, calc_endminsec, eventid) " +
                                            "VALUES (@spawnpoint, @latitude, @longitude, @earliestUnseen, @LastScanned, @spawnDef, @calcEndminsec, " +
                                            "(select id from trs_event where now() between event_start and " +
                                            "event_end order by event_start desc limit 1)) " +
                                            "ON DUPLICATE KEY UPDATE " +
                                            "last_scanned=VALUES(last_scanned), " +
                                            "earliest_unseen=LEAST(earliest_unseen, VALUES(earliest_unseen)), " +
                                            "spawndef=if(((select id from trs_event where now() between event_start and event_end order " +
                                            "by event_start desc limit 1)=1 and eventid=1) or (select id from trs_event where now() between " +
                                            "event_start and event_end order by event_start desc limit 1)<>1 and eventid<>1, VALUES(spawndef), " +
                                            "spawndef), " +
                                            "calc_endminsec=VALUES(calc_endminsec)";

                String spawnpointsUnseenQuery = "INSERT INTO trs_spawn (spawnpoint, latitude, longitude, earliest_unseen, last_non_scanned, spawndef, " +
                                                "eventid) VALUES (@spawnpoint, @latitude, @longitude, @earliestUnseen, @LastNonScanned, @spawnDef, " +
                                                "(select id from trs_event where now() between event_start and " +
                                                "event_end order by event_start desc limit 1)) " +
                                                "ON DUPLICATE KEY UPDATE " +
                                                "spawndef=if(((select id from trs_event where now() between event_start and event_end order " +
                                                "by event_start desc limit 1)=1 and eventid=1) or (select id from trs_event where now() between " +
                                                "event_start and event_end order by event_start desc limit 1)<>1 and eventid<>1, VALUES(spawndef), " +
                                                "spawndef), " +
                                                "last_non_scanned=VALUES(last_non_scanned)";
                var spawnIds = cells.SelectMany(cell => cell.WildPokemon).Select(poke => Convert.ToInt64(poke.SpawnPointId, 16));

                var spawnIdsString = String.Join(", ", spawnIds).Trim(',');
                var getSpawnpointsQuery = $"SELECT spawnpoint, spawndef, calc_endminsec FROM trs_spawn WHERE spawnpoint in ({spawnIdsString})";
                var dbSpawnpoints = context.Spawnpoints.FromSqlRaw(getSpawnpointsQuery);

                foreach (var cell in cells)
                {
                    foreach (var wild in cell.WildPokemon)
                    {
                        var currentSpawnpointId = Convert.ToInt64(wild.SpawnPointId, 16);
                        var id = new MySqlParameter("spawnpoint", currentSpawnpointId);

                        var cellLatLng = new S2CellId((ulong)Convert.ToInt64(wild.SpawnPointId + "00000", 16)).ToLatLng();
                        var latitude = new MySqlParameter("latitude", cellLatLng.LatDegrees);
                        var longitude = new MySqlParameter("longitude", cellLatLng.LngDegrees);

                        var despawnTime = wild.TimeTillHiddenMs;
                        var minPos = getCurrentSpawnDefPosition();

                        Spawnpoint currentDbSpawnpoint = dbSpawnpoints.FirstOrDefault(s => s.spawnpoint == currentSpawnpointId);
                        int oldSpawnDef = currentDbSpawnpoint != null ? currentDbSpawnpoint.spawndef : int.MinValue;
                        MySqlParameter newSpawnDef;
                        if (oldSpawnDef != int.MinValue)
                        {
                            newSpawnDef = new MySqlParameter("spawnDef", getSpawnDefWithMinPos(oldSpawnDef, minPos));
                        }
                        else
                        {
                            newSpawnDef = new MySqlParameter("spawnDef", getSpawnDefWithMinPos(240, minPos));
                        }
                        if (0 <= despawnTime && despawnTime <= 90000)
                        {
                            var earliestUnseen = new MySqlParameter("earliestUnseen", despawnTime);
                            var lastScanned = new MySqlParameter("LastScanned", ToMySQLDateTime(DateTime.UtcNow));
                            var calcEndTime = new MySqlParameter("calcEndminsec", DateTime.UtcNow.AddMilliseconds(despawnTime).ToString("mm:ss"));

                            context.Database.ExecuteSqlRaw(spawnpointsQuery, id, latitude, longitude, earliestUnseen, lastScanned, newSpawnDef, calcEndTime);
                        }
                        else
                        {
                            var earliestUnseen = new MySqlParameter("earliestUnseen", 99999999);
                            var lastScanned = new MySqlParameter("LastNonScanned", DateTime.UtcNow);

                            context.Database.ExecuteSqlRaw(spawnpointsUnseenQuery, id, latitude, longitude, earliestUnseen, lastScanned, newSpawnDef);
                        }
                    }
                }
            }
        }

        private int getCurrentSpawnDefPosition()
        {
            var minute = DateTime.Now.Minute;

            if (minute < 15)
            {
                return 4;
            }
            if (minute < 30)
            {
                return 5;
            }
            if (minute < 45)
            {
                return 6;
            }
            if (minute < 60)
            {
                return 7;
            }

            return -1;
        }

        private int getSpawnDefWithMinPos(int oldSpawnDef, int minPos)
        {
            var bitArray =  Enumerable.Range(0, 8)
                            .Select(bitIndex => 1 << bitIndex)
                            .Select(bitMask => (oldSpawnDef & bitMask) == bitMask)
                            .ToArray();
            switch (minPos)
            {
                case 4:
                    bitArray[0] = false;
                    bitArray[4] = true;
                    break;
                case 5:
                    bitArray[1] = false;
                    bitArray[5] = true;
                    break;
                case 6:
                    bitArray[2] = false;
                    bitArray[6] = true;
                    break;
                case 7:
                    bitArray[3] = false;
                    bitArray[7] = true;
                    break;
            }
            int value = 0;
            int expo = 0;
            for (int i = bitArray.Length-1; i >= 0; i--)
            {
                value += 2 ^ expo * (bitArray[i] ? 1 : 0);
                expo++;
            }
            return value;
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
                                                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
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
                                                GetQuestTaskText(quest.QuestType, quest.Goal.Condition, quest.Goal.Target, quest.TemplateId), // Task text
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
            if (gym.GymDisplay == null)
            {
                return;
            }

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
                                            ToMySQLDateTime(DateTime.UtcNow),
                                            gym.IsExRaidEligible,
                                            gym.IsArScanEligible);

            queryGymDetails = String.Format(queryGymDetails,
                                            gym.FortId,
                                            "\"unknown\"",
                                            MySQLEscape(gym.ImageUrl),
                                            ToMySQLDateTime(DateTime.UtcNow));

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
                parameters.Add(ToMySQLDateTime(DateTime.UtcNow));
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
                parameters.Add(ToMySQLDateTime(DateTime.UtcNow));
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

        public void AddCells(List<ClientMapCellProto> cells)
        {
            using (var context = new RocketMapContext())
            {
                foreach (ClientMapCellProto cell in cells)
                {
                    String query = "INSERT INTO trs_s2cells (id, level, center_latitude, center_longitude, updated) " +
                            "VALUES ({0}, {1}, {2}, {3}, {4}) " +
                            "ON DUPLICATE KEY UPDATE updated=VALUES(updated)";

                    List<Object> parameters = new List<Object>();
                    parameters.Add(cell.S2CellId);
                    parameters.Add(15);
                    ulong cellId = cell.S2CellId;
                    if (cellId < 0) {
                        cellId = (ulong)(cellId + Math.Pow(2, 64));
                        }

                    var s2Cell = new S2CellId(cellId).ToLatLng();
                    parameters.Add(s2Cell.LatDegrees);
                    parameters.Add(s2Cell.LngDegrees);
                    parameters.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    try
                    {
                        query = String.Format(query, parameters.ToArray());

                        context.Database.ExecuteSqlRaw(query);
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.Message);
                        Log.Information(e.StackTrace);
                        Log.Information($"Object: {JsonSerializer.Serialize(cell)} \n\n Cell Query: {query} \n\n Params: {JsonSerializer.Serialize(parameters)}");
                    }
                }
            }
        }

        public void AddWeather(List<ClientWeatherProto> weatherList, int timeOfDay)
        {
            using (var context = new RocketMapContext())
            {
                foreach (ClientWeatherProto weather in weatherList)
                {
                    String query =  "INSERT INTO weather (s2_cell_id, latitude, longitude, cloud_level, rain_level, wind_level, " +
                                    "snow_level, fog_level, wind_direction, gameplay_weather, severity, warn_weather, world_time, " +
                                    "last_updated) " +
                                    "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, \"{13}\") " +
                                    "ON DUPLICATE KEY UPDATE fog_level=VALUES(fog_level), cloud_level=VALUES(cloud_level), " +
                                    "snow_level=VALUES(snow_level), wind_direction=VALUES(wind_direction), " +
                                    "world_time=VALUES(world_time), gameplay_weather=VALUES(gameplay_weather), " +
                                    "last_updated=VALUES(last_updated)";

                    if(weather.DisplayWeather == null)
                    {
                        continue;
                    }

                    List<Object> parameters = new List<Object>();
                    parameters.Add(weather.S2CellId);
                    var s2CellId = new S2CellId((ulong) weather.S2CellId).ToLatLng();
                    parameters.Add(s2CellId.LatDegrees);
                    parameters.Add(s2CellId.LngDegrees);
                    parameters.Add((int)weather.DisplayWeather.CloudLevel);
                    parameters.Add((int)weather.DisplayWeather.RainLevel);
                    parameters.Add((int)weather.DisplayWeather.WindLevel);
                    parameters.Add((int)weather.DisplayWeather.SnowLevel);
                    parameters.Add((int)weather.DisplayWeather.FogLevel);
                    parameters.Add((int)weather.DisplayWeather.WindDirection);
                    parameters.Add((int) weather.GameplayWeather.GameplayCondition);
                    parameters.Add(0);
                    parameters.Add(0);
                    parameters.Add(timeOfDay);
                    parameters.Add(ToMySQLDateTime(DateTime.UtcNow));

                    try
                    {
                        query = String.Format(query, parameters.ToArray());

                        context.Database.ExecuteSqlRaw(query);
                    }
                    catch (Exception e)
                    {
                        Log.Information(e.Message);
                        Log.Information(e.StackTrace);
                        Log.Information($"Object: {JsonSerializer.Serialize(weather)} \n\n Weather Query: {query} \n\n Params: {JsonSerializer.Serialize(parameters)}");
                    }
                }
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

        private String GetQuestTaskText(QuestType type, RepeatedField<QuestConditionProto> conditions, int target, String templateId)
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
                    parameters.Add("");
                    parameters.Add("");
                    parameters.Add("");
                    parameters.Add("");

                    text = "Catch {0}{1} {2}Pokemon{3}";

                    foreach(QuestConditionProto condition in conditions)
                    {
                        switch (condition.Type)
                        {
                            case QuestConditionProto.Types.ConditionType.WithPokemonType:
                                if (condition.WithPokemonType.PokemonType.Count == 1)
                                {
                                    String temp = String.Join("-, ", condition.WithPokemonType.PokemonType.Select(type => RocketMapUtils.shared.GetPokemonType((int)type)));
                                    parameters[2] = $"{temp}-type ";
                                }
                                break;
                            case QuestConditionProto.Types.ConditionType.WithPokemonCategory:
                                //TODO: Add translation
                                if (condition.WithPokemonCategory.PokemonIds.Count > 0)
                                {
                                    text = "Catch {0} {1}";
                                    parameters[1] = String.Join(", ", condition.WithPokemonCategory.PokemonIds.Select(pokemon => RocketMapUtils.shared.GetPokemonName((int)pokemon)));
                                }
                                break;
                            case QuestConditionProto.Types.ConditionType.WithWeatherBoost:
                                parameters[3] = " with weather boost";
                                break;
                            case QuestConditionProto.Types.ConditionType.WithUniquePokemon:
                                parameters[1] = " different species of";
                                break;
                            case QuestConditionProto.Types.ConditionType.WithPokemonAlignment:
                                if (condition.WithPokemonAlignment.Alignment.Count == 1)
                                {
                                    switch (condition.WithPokemonAlignment.Alignment[0])
                                    {
                                        case PokemonDisplayProto.Types.Alignment.Shadow:
                                            parameters[1] = " shadow";
                                            break;
                                        case PokemonDisplayProto.Types.Alignment.Purified:
                                            parameters[1] = " purified";
                                            break;
                                    }
                                }
                                break;
                        }
                    }

                    break;
                case QuestType.QuestSpinPokestop:
                    if(conditions.Any(c => c.Type == QuestConditionProto.Types.ConditionType.WithUniquePokestop)){
                        text = "Spin {0} Pokestops you haven't visited before.";
                    } else
                    {
                        text = "Spin {0} Pokestops or Gyms.";
                    }
                    break;
                case QuestType.QuestCompleteGymBattle:
                    if (conditions.Any(c => c.Type == QuestConditionProto.Types.ConditionType.WithWinGymBattleStatus))
                    {
                        text = "Win {0} Gym Battles.";
                    } else
                    {
                        if (conditions.Any(c => c.Type == QuestConditionProto.Types.ConditionType.WithSuperEffectiveCharge))
                        {
                            text = "Use a supereffective Charged Attack in {0} Gym battles.";
                        }
                    }
                    break;
                case QuestType.QuestCompleteRaidBattle:
                    QuestConditionProto raidCondition = conditions.FirstOrDefault(c => c.WithRaidLevel != null);
                    if (raidCondition != null)
                    {
                        text = "Win {0} Raids.";
                        switch (raidCondition.WithRaidLevel.RaidLevel[0])
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
                    QuestConditionProto berryCondition = conditions.FirstOrDefault(c => c.WithItem != null);
                    if (berryCondition != null)
                    {
                        berrie = RocketMapUtils.shared.GetItemName((int)berryCondition.WithItem.Item).Replace("Berry", "");
                    }
                    parameters.Add(berrie);
                    break;
                case QuestType.QuestLandThrow:
                    parameters.Add("");
                    parameters.Add("");
                    parameters.Add("");

                    text = "Make {0} {1}{2}Throws{3}.";

                    QuestConditionProto throwCondition = conditions.FirstOrDefault(c => c.WithThrowType != null);
                    if (throwCondition != null)
                    {
                        switch (throwCondition.WithThrowType.ThrowType)
                        {
                            case HoloActivityType.ActivityCatchNiceThrow:
                                parameters[1] = "Nice ";
                                break;
                            case HoloActivityType.ActivityCatchGreatThrow:
                                parameters[1] = "Great ";
                                break;
                            case HoloActivityType.ActivityCatchExcellentThrow:
                                parameters[1] = "Excellent ";
                                break;
                            case HoloActivityType.ActivityCatchCurveball:
                                parameters[1] = "Curveball ";
                                break;
                        }
                    }
                    if (conditions.Any(c => c.Type == QuestConditionProto.Types.ConditionType.WithCurveBall))
                    {
                        parameters[2] = "Curveball ";
                    }
                    if (conditions.Any(c => c.Type == QuestConditionProto.Types.ConditionType.WithThrowTypeInARow))
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
                text = text.Replace("Grunts", "Grunt");
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
