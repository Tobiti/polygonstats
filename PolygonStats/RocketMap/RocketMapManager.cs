using Microsoft.EntityFrameworkCore;
using POGOProtos.Rpc;
using System;
using Serilog;
using System.Text.Json;

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
                if(encounter.Pokemon.TimeTillHiddenMs > 0)
                {
                    disappearTime = disappearTime.AddMilliseconds(encounter.Pokemon.TimeTillHiddenMs);
                } else
                {
                    disappearTime = disappearTime.AddMinutes(20);
                }
                String query = $"INSERT INTO pokemon (encounter_id, spawnpoint_id, pokemon_id, latitude, longitude, disappear_time, individual_attack, individual_defense, individual_stamina, move_1, move_2, cp, cp_multiplier, weight, height, gender, form, costume, catch_prob_1, catch_prob_2, catch_prob_3, rating_attack, rating_defense, weather_boosted_condition, last_modified, fort_id, cell_id, seen_type) VALUES({encounter.Pokemon.EncounterId}, {encounter.Pokemon.SpawnPointId}, {(int)encounter.Pokemon.Pokemon.PokemonId}, {encounter.Pokemon.Latitude}, {encounter.Pokemon.Longitude}, \"{disappearTime.ToString("yyyy-MM-dd HH:mm:ss")}\", {encounter.Pokemon.Pokemon.IndividualAttack}, {encounter.Pokemon.Pokemon.IndividualDefense}, {encounter.Pokemon.Pokemon.IndividualStamina}, {(int)encounter.Pokemon.Pokemon.Move1}, {(int)encounter.Pokemon.Pokemon.Move2}, {encounter.Pokemon.Pokemon.Cp}, {encounter.Pokemon.Pokemon.CpMultiplier}, {encounter.Pokemon.Pokemon.WeightKg}, {encounter.Pokemon.Pokemon.HeightM}, {(int)encounter.Pokemon.Pokemon.PokemonDisplay.Gender}, {(int)encounter.Pokemon.Pokemon.PokemonDisplay.Form}, {(int)encounter.Pokemon.Pokemon.PokemonDisplay.Costume}, {encounter.CaptureProbability.CaptureProbability[0]}, {encounter.CaptureProbability.CaptureProbability[1]}, {encounter.CaptureProbability.CaptureProbability[2]}, \"\", \"\", {(int)encounter.Pokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition}, \"{encounter.Pokemon.LastModifiedMs.ToString("yyyy-MM-dd HH:mm:ss")}\", NULL, {encounter.Pokemon.Pokemon.CapturedS2CellId}, \"{SeenType.wild.ToString("g")}\") ON DUPLICATE KEY UPDATE encounter_id={encounter.Pokemon.EncounterId}";
                try
                {
                    context.Database.ExecuteSqlRaw(query);
                } catch (Exception e)
                {
                    Log.Information(e.Message);
                    Log.Information(e.StackTrace);
                    Log.Information($"Object: {JsonSerializer.Serialize(encounter)} \n Query: {query}");

                }
            }
        }
    }
}
