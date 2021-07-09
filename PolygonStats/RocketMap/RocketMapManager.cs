using Microsoft.EntityFrameworkCore;
using POGOProtos.Rpc;
using System;
using Serilog;
using System.Text.Json;
using System.Collections.Generic;

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
                                        "VALUES (\"{0}\", {1}, {2}, {3}, \"{4}\", \"{5}\", \"{6}\", \"{7}\", {8}, {9}, {10}, {11}, {12}) " +
                                        "ON DUPLICATE KEY UPDATE last_updated=VALUES(last_updated), lure_expiration=VALUES(lure_expiration), " +
                                        "last_modified=VALUES(last_modified), latitude=VALUES(latitude), longitude=VALUES(longitude), " +
                                        "active_fort_modifier=VALUES(active_fort_modifier), incident_start=VALUES(incident_start), " +
                                        "incident_expiration=VALUES(incident_expiration), incident_grunt_type=VALUES(incident_grunt_type), " +
                                        "is_ar_scan_eligible=VALUES(is_ar_scan_eligible), image=VALUES(image) ";
                        try
                        {
                            query = String.Format(query, fort.FortId, fort.Enabled, fort.Latitude, fort.Longitude, ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs)), 
                                                        ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs).AddMinutes(30)), 
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
                                        "VALUES (\"{0}\", {1}, \"{2}\", {3}) " +
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
                                            gym.ImageUrl,
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
            parameters.Add(gym.RaidInfo.RaidLevel);
            parameters.Add(ToMySQLDateTime(UnixTimeStampToDateTime(gym.RaidInfo.RaidSpawnMs)));
            parameters.Add(ToMySQLDateTime(UnixTimeStampToDateTime(gym.RaidInfo.RaidBattleMs)));
            parameters.Add(ToMySQLDateTime(UnixTimeStampToDateTime(gym.RaidInfo.RaidEndMs)));

            if (gym.RaidInfo.RaidPokemon != null)
            {
                parameters.Add((int)gym.RaidInfo.RaidPokemon.PokemonId);
                parameters.Add(gym.RaidInfo.RaidPokemon.Cp);
                parameters.Add(gym.RaidInfo.RaidPokemon.Move1);
                parameters.Add(gym.RaidInfo.RaidPokemon.Move2);
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
                query = String.Format(query, parameters);

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
    }
}
