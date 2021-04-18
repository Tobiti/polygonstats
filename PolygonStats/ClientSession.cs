using System;
using System.Text;
using System.Net.Sockets;
using NetCoreServer;
using System.Text.Json;
using POGOProtos.Rpc;
using Google.Protobuf.Collections;

namespace PolygonStats
{
    class ClientSession : TcpSession
    {
        private string messageBuffer = "";
        private string accountName = null;
        public ClientSession(TcpServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"Polygon TCP session with Id {Id} connected!");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Polygon TCP session with Id {Id} disconnected!");
            if (accountName != null)
            {
                StatManager.sharedInstance.removeEntry(accountName);
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string currentMessage = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            //Console.WriteLine($"Message: {currentMessage}");

            messageBuffer += currentMessage;
            var jsonStrings = messageBuffer.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string jsonString in jsonStrings)
            {
                if (!jsonString.Equals(""))
                {
                    string trimedJsonString = jsonString.Trim('\r', '\n'); ;
                    try
                    {
                        messageBuffer = "";
                        MessageObject message = JsonSerializer.Deserialize<MessageObject>(trimedJsonString);
                        StringBuilder sb = new StringBuilder();
                        foreach (Payload payload in message.payloads)
                        {
                            if (accountName == null)
                            {
                                accountName = payload.account_name;
                                StatManager.sharedInstance.getEntry(accountName);
                            }
                            handlePayload(payload);
                        }
                        if (sb.Length > 0)
                        {
                            Console.WriteLine(sb.ToString().Trim('\r', '\n'));
                        }
                    }
                    catch (JsonException e)
                    {
                        messageBuffer = jsonString;
                        //Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        private void handlePayload(Payload payload)
        {
            switch (payload.getMethodType())
            {
                case Method.CatchPokemon:
                    CatchPokemonOutProto catchPokemonProto = CatchPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    if (catchPokemonProto.PokemonDisplay != null)
                    {
                        Console.WriteLine($"Pokemon {catchPokemonProto.DisplayPokedexId.ToString("G")} Status: {catchPokemonProto.Status.ToString("G")}.");
                        StatManager.sharedInstance.addCatchedPokemon(payload.account_name, catchPokemonProto);
                    }
                    break;
                case Method.GymFeedPokemon:
                    GymFeedPokemonOutProto feedPokemonProto = GymFeedPokemonOutProto.Parser.ParseFrom(payload.getDate());
                    if (feedPokemonProto.Result == GymFeedPokemonOutProto.Types.Result.Success)
                    {
                        Stats entry = StatManager.sharedInstance.getEntry(payload.account_name);
                        entry.addXp(feedPokemonProto.XpAwarded);
                        entry.addStardust(feedPokemonProto.StardustAwarded);
                    }
                    break;
                case Method.CompleteQuest:
                    CompleteQuestOutProto questProto = CompleteQuestOutProto.Parser.ParseFrom(payload.getDate());
                    if (questProto.Status == CompleteQuestOutProto.Types.Status.Success)
                    {
                        processQuestRewards(payload.account_name, questProto.Quest.Quest.QuestRewards);
                    }
                    break;
                case Method.CompleteQuestStampCard:
                    CompleteQuestStampCardOutProto completeQuestStampCardProto = CompleteQuestStampCardOutProto.Parser.ParseFrom(payload.getDate());
                    if (completeQuestStampCardProto.Status == CompleteQuestStampCardOutProto.Types.Status.Success)
                    {
                        processQuestRewards(payload.account_name, completeQuestStampCardProto.Reward);
                    }
                    break;
                case Method.GetHatchedEggs:
                    GetHatchedEggsOutProto getHatchedEggsProto = GetHatchedEggsOutProto.Parser.ParseFrom(payload.getDate());
                    if (getHatchedEggsProto.Success)
                    {
                        processHatchedEggReward(payload.account_name, getHatchedEggsProto);
                    }
                    break;
                case Method.FortSearch:
                    FortSearchOutProto fortSearchProto = FortSearchOutProto.Parser.ParseFrom(payload.getDate());
                    if (fortSearchProto.Result == FortSearchOutProto.Types.Result.Success)
                    {
                        Stats entry = StatManager.sharedInstance.getEntry(payload.account_name);
                        entry.addSpinnedPokestop();
                        entry.addXp(fortSearchProto.XpAwarded);
                    }
                    break;
                default:
                    //Console.WriteLine($"Account: {payload.account_name}");
                    //Console.WriteLine($"Type: {payload.getMethodType().ToString("G")}");
                    break;
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }

        private void processQuestRewards(string acc, RepeatedField<QuestRewardProto> rewards)
        {
            Stats entry = StatManager.sharedInstance.getEntry(acc);
            foreach (QuestRewardProto reward in rewards)
            {
                if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Exp)
                {
                    entry.addXp(reward.Exp);
                }
                if (reward.RewardCase == QuestRewardProto.RewardOneofCase.Stardust)
                {
                    entry.addStardust(reward.Stardust);
                }
            }
        }
        private void processHatchedEggReward(string acc, GetHatchedEggsOutProto getHatchedEggsProto)
        {
            Stats entry = StatManager.sharedInstance.getEntry(acc);
            int xpSum = 0;
            foreach (int reward in getHatchedEggsProto.ExpAwarded)
            {
                xpSum += reward;
            }
            entry.addXp(xpSum);

            int stardustSum = 0;
            foreach (int reward in getHatchedEggsProto.StardustAwarded)
            {
                stardustSum += reward;
            }
            entry.addStardust(stardustSum);
        }
    }
}
