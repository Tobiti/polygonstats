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
                String query = $"INSERT INTO pokemon (encounter_id, spawnpoint_id, pokemon_id, latitude, longitude, disappear_time, individual_attack, individual_defense, individual_stamina, move_1, move_2, cp, cp_multiplier, weight, height, gender, form, costume, catch_prob_1, catch_prob_2, catch_prob_3, rating_attack, rating_defense, weather_boosted_condition, last_modified, fort_id, cell_id, seen_type) VALUES({encounter.Pokemon.EncounterId}, 1, {(int)encounter.Pokemon.Pokemon.PokemonId}, {encounter.Pokemon.Latitude}, {encounter.Pokemon.Longitude}, \"{disappearTime.ToString("yyyy-MM-dd HH:mm:ss")}\", {encounter.Pokemon.Pokemon.IndividualAttack}, {encounter.Pokemon.Pokemon.IndividualDefense}, {encounter.Pokemon.Pokemon.IndividualStamina}, {(int)encounter.Pokemon.Pokemon.Move1}, {(int)encounter.Pokemon.Pokemon.Move2}, {encounter.Pokemon.Pokemon.Cp}, {encounter.Pokemon.Pokemon.CpMultiplier}, {encounter.Pokemon.Pokemon.WeightKg}, {encounter.Pokemon.Pokemon.HeightM}, {(int)encounter.Pokemon.Pokemon.PokemonDisplay.Gender}, {(int)encounter.Pokemon.Pokemon.PokemonDisplay.Form}, {(int)encounter.Pokemon.Pokemon.PokemonDisplay.Costume}, {encounter.CaptureProbability.CaptureProbability[0]}, {encounter.CaptureProbability.CaptureProbability[1]}, {encounter.CaptureProbability.CaptureProbability[2]}, \"\", \"\", {(int)encounter.Pokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition}, \"{UnixTimeStampToDateTime(encounter.Pokemon.LastModifiedMs).ToString("yyyy-MM-dd HH:mm:ss")}\", NULL, {encounter.Pokemon.Pokemon.CapturedS2CellId}, \"{SeenType.wild.ToString("g")}\") ON DUPLICATE KEY UPDATE encounter_id={encounter.Pokemon.EncounterId}";
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
                        String query = $"INSERT INTO pokestop (pokestop_id, enabled, latitude, longitude, last_modified, lure_expiration, active_fort_modifier, last_updated, `name`, image, incident_start, incident_expiration, incident_grunt_type, is_ar_scan_eligible) VALUES({fort.FortId}, {fort.Enabled}, {fort.Latitude}, {fort.Longitude}, \"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.LastModifiedMs))}\", \"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.CooldownCompleteMs))}\",  {(fort.ActiveFortModifier.Count > 0 ? (int)fort.ActiveFortModifier[0] : 0)}, \"{ToMySQLDateTime(DateTime.UtcNow)}\", \"{fort.GeostoreSuspensionMessageKey}\", \"{fort.ImageUrl}\", \"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.PokestopDisplay.IncidentStartMs))}\", \"{ToMySQLDateTime(UnixTimeStampToDateTime(fort.PokestopDisplay.IncidentExpirationMs))}\", {(int)fort.PokestopDisplay.CharacterDisplay.Character}, {fort.IsArScanEligible}) ON DUPLICATE KEY UPDATE pokestop_id={fort.FortId}";
                        try
                        {
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
