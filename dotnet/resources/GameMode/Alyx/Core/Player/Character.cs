﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GTANetworkAPI;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Alyx.Houses;
using Alyx.GUI;
using MySqlConnector;
using AlyxSDK;
using Alyx.Roulette;

namespace Alyx.Core.Character
{
    public class Character : CharacterData
    {
        private static nLog Log = new nLog("Character");
        private static Random Rnd = new Random();
        
        public void Spawn(Player player)
        {
            try
            {
                NAPI.Task.Run(() =>
                {
                    try
                    {
                        player.SetSharedData("IS_MASK", false);

                        // Logged in state, money, phone init
                        Trigger.ClientEvent(player, "loggedIn");
                        player.SetData("LOGGED_IN", true);

                        Trigger.ClientEvent(player, "UpdateMoney", Money);
                        #region LastBonus
                        if (IsBonused)
                        {
                            Trigger.ClientEvent(player, "UpdateLastBonus", $"следующий бонус можно получить только завтра"); //todo lastbonus
                        }
                        else
                        {
                            DateTime date = new DateTime((new DateTime().AddMinutes(Main.oldconfig.LastBonusMin - LastBonus)).Ticks);
                            var hour = date.Hour;
                            var min = date.Minute;
                            Trigger.ClientEvent(player, "UpdateLastBonus", $"{hour}ч. {min}м."); //todo lastbonus
                        }
                        #endregion
                        Trigger.ClientEvent(player, "UpdateEat", Main.Players[player].Eat);
                        Trigger.ClientEvent(player, "UpdateWater", Main.Players[player].Water);
                        Trigger.ClientEvent(player, "UpdateBank", MoneySystem.Bank.Accounts[Bank].Balance);
                        Trigger.ClientEvent(player, "freeze", false);
                        Trigger.ClientEvent(player, "initPhone");
                        Jobs.WorkManager.load(player);

                        // Skin, Health, Armor, RemoteID
                        IsAlive = true; //прочекнуть
                        player.SetSkin((Gender) ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01);
                        player.Health = (Health > 5) ? Health : 5;
                        player.Armor = Armor;

                        player.SetSharedData("REMOTE_ID", player.Value);
                        player.SetSharedData("PERSON_ID", PersonID);

                        Voice.Voice.PlayerJoin(player);

                        player.SetSharedData("voipmode", -1);

                       // if (Fractions.Manager.FractionTypes[FractionID] == 1 || AdminLVL > 0) Fractions.GangsCapture.LoadBlips(player);
                        if (WantedLVL != null) Trigger.ClientEvent(player, "setWanted", WantedLVL.Level);
                        
                        player.SetData("RESIST_STAGE", 0);
                        player.SetData("RESIST_TIME", 0);
                        if (AdminLVL > 0)player.SetSharedData("IS_ADMIN", true);
                        player.SetSharedData("ALVL", AdminLVL);

                        Dashboard.sendStats(player);
                        Dashboard.sendItems(player);
                        if(Main.Players[player].LVL == 0) {
                            NAPI.Task.Run(() => { try { Trigger.ClientEvent(player, "disabledmg", true); } catch { } }, 5000);
                        }
                       
                        House house = HouseManager.GetHouse(player);
                        if (house != null)
                        {
                            // House blips & checkpoints
                            house.PetName = Main.Players[player].PetName;

                            Trigger.ClientEvent(player, "changeBlipColor", house.blip, 73);

                            Trigger.ClientEvent(player, "createCheckpoint", 333, 1, GarageManager.Garages[house.GarageID].Position - new Vector3(0, 0, 1.12), 1, NAPI.GlobalDimension, 220, 220, 0);
                            Trigger.ClientEvent(player, "createGarageBlip", GarageManager.Garages[house.GarageID].Position);
                        }
                        
                        if (!Customization.CustomPlayerData.ContainsKey(UUID) || !Customization.CustomPlayerData[UUID].IsCreated)
                        {
                            Trigger.ClientEvent(player, "spawnShow", false);
                            Customization.CreateCharacter(player);
                        }
                        else
                        {
                            try
                            {
                                //NAPI.Entity.SetEntityPosition(player, Main.Players[player].SpawnPos);
                                List<bool> prepData = new List<bool>
                                {
                                    true,
                                    (FractionID > 0) ? true : false,
                                    (house != null || HotelID != -1) ? true : false,
                                };
                                string json = JsonConvert.SerializeObject(prepData);
                                Trigger.ClientEvent(player, "spawnShow", json);
                                Customization.ApplyCharacter(player);
                            }
                            catch { }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Write($"EXCEPTION AT \"Spawn.NAPI.Task.Run\":\n" + e.ToString(), nLog.Type.Error);
                    }
                });

                if (Warns > 0 && DateTime.Now > Unwarn)
                {
                    Warns--;

                    if (Warns > 0)
                        Unwarn = DateTime.Now.AddDays(14);
                    Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, $"Одно предупреждение было снято. У Вас осталось {Warns}", 3000);
                }

                if (!Dashboard.isopen.ContainsKey(player))
                    Dashboard.isopen.Add(player, false);

                nInventory.Check(UUID);
                if (nInventory.Find(UUID, ItemType.BagWithMoney) != null)
                    nInventory.Remove(player, ItemType.BagWithMoney, 1);
                if (nInventory.Find(UUID, ItemType.BagWithDrill) != null)
                    nInventory.Remove(player, ItemType.BagWithDrill, 1);
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"Spawn\":\n" + e.ToString());
            }
        }

        public async Task Load(Player player, int uuid)
        {
            try
            {
                if (Main.Players.ContainsKey(player))
                    Main.Players.Remove(player);

                DataTable result = await MySQL.QueryReadAsync($"SELECT * FROM `characters` WHERE uuid={uuid}");
                if (result == null || result.Rows.Count == 0) return;

                NAPI.Task.Run(() =>
                {
                    foreach (DataRow Row in result.Rows)
                    {
                        UUID = Convert.ToInt32(Row["uuid"]);
                        PersonID = Convert.ToInt32(Row["personid"]); //todo PersonID
                        FirstName = Convert.ToString(Row["firstname"]);
                        LastName = Convert.ToString(Row["lastname"]);
                        Gender = Convert.ToBoolean(Row["gender"]);
                        Health = Convert.ToInt32(Row["health"]);
                        Armor = Convert.ToInt32(Row["armor"]);
                        LVL = Convert.ToInt32(Row["lvl"]);
                        EXP = Convert.ToInt32(Row["exp"]);
                        Eat = Convert.ToInt32(Row["eat"]);
                        Water = Convert.ToInt32(Row["water"]);
                        Age = Convert.ToInt32(Row["age"]);
                        Money = Convert.ToInt64(Row["money"]);
                        Bank = Convert.ToInt32(Row["bank"]);
                        WorkID = Convert.ToInt32(Row["work"]);
                        FractionID = Convert.ToInt32(Row["fraction"]);
                        FractionLVL = Convert.ToInt32(Row["fractionlvl"]);
                        ArrestTime = Convert.ToInt32(Row["arrest"]);
                        Fines = Convert.ToInt32(Row["fines"]); //todo fines 
                        DemorganTime = Convert.ToInt32(Row["demorgan"]);
                        DemroganReason = Convert.ToString(Row["demorganreason"]);
                        DemorganAdmin = Convert.ToString(Row["demorganadmin"]);
                        WantedLVL = JsonConvert.DeserializeObject<WantedLevel>(Row["wanted"].ToString());
                        WorksStats = JsonConvert.DeserializeObject<List<WorkStats>>(Row["workstats"].ToString()); //todo workstats
                        if (WorksStats == null) WorksStats = new List<WorkStats>();
                        BizIDs = JsonConvert.DeserializeObject<List<int>>(Row["biz"].ToString());
                        AdminLVL = Convert.ToInt32(Row["adminlvl"]);
                        Licenses = JsonConvert.DeserializeObject<List<bool>>(Row["licenses"].ToString());
                        Unwarn = ((DateTime)Row["unwarn"]);
                        Unmute = Convert.ToInt32(Row["unmute"]);
                        Warns = Convert.ToInt32(Row["warns"]);
                        LastVeh = Convert.ToString(Row["lastveh"]);
                        OnDuty = Convert.ToBoolean(Row["onduty"]);
                        LastHourMin = Convert.ToInt32(Row["lasthour"]);
                        LastBonus = Convert.ToInt32(Row["lastbonus"]); //todo lastbonus
                        IsBonused = Convert.ToBoolean(Row["isbonused"]); //todo lastbonus
                        LuckyWheelSpins = Convert.ToInt32(Row["luckywheelspins"]); //todo luckywheel
                        HotelID = Convert.ToInt32(Row["hotel"]);
                        HotelLeft = Convert.ToInt32(Row["hotelleft"]);
                        Contacts = JsonConvert.DeserializeObject<Dictionary<int, string>>(Row["contacts"].ToString());
                        Achievements = JsonConvert.DeserializeObject<List<bool>>(Row["achiev"].ToString());
                        if (Achievements == null)
                        {
                            Achievements = new List<bool>();
                            for (uint i = 0; i != 401; i++) Achievements.Add(false);
                        }
                        Sim = Convert.ToInt32(Row["sim"]);
                        PetName = Convert.ToString(Row["PetName"]);
                        CreateDate = ((DateTime)Row["createdate"]);
                        if (!string.IsNullOrEmpty(Row["cooldown"].ToString()))
                            Cooldown = ((DateTime)Row["cooldown"]);
                        else
                        {
                            Cooldown = DateTime.Now;
                            Cooldown.AddHours(-25);
                        }
                        if (!CaseController.CasesPrizeList.ContainsKey(UUID))
                        {
                            List<CaseController.CaseItem> CaseList = new List<CaseController.CaseItem>();
                            CaseController.CasesPrizeList.Add(UUID, CaseList);
                            MySQL.Query($"INSERT INTO `caseprize` (`UUID`, `Prize`) VALUES({UUID},'{JsonConvert.SerializeObject(CaseController.CasesPrizeList[UUID])}')");
                        }
                        SpawnPos = JsonConvert.DeserializeObject<Vector3>(Row["pos"].ToString());
                        if (Row["pos"].ToString().Contains("NaN"))
                        {
                            Log.Debug("Detected wrong coordinates!", nLog.Type.Warn);
                            if (LVL <= 1) SpawnPos = new Vector3(-490.1876, -694.6452, 33.213594); // На спавне новичков
                            else SpawnPos = new Vector3(-490.1876, -694.6452, 33.213594); //На спавне новичков у мэрии 
                        }
                    }
                    player.Name = FirstName + "_" + LastName;
                    Main.Players.Add(player, this);
                    CheckAchievements(player);
                   // GameLog.Connected(player.Name, UUID, player.GetData<string>("RealSocialClub"), player.GetData<string>("RealHWID"), player.Value, player.Address);
                    Spawn(player);
                });
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"Load\":\n" + e.ToString());
            }
        }


        public static void CheckAchievements(Player player) {
            try {
                if (Main.Players[player].Achievements[0] == true && Main.Players[player].Achievements[1] == false || Main.Players[player].Achievements[1] == true && Main.Players[player].Achievements[2] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые шаги", 100, "Купи в магазине 24/7 пачку чипсов и отнеси Гарри");
                }
                else if (Main.Players[player].Achievements[2] == true && Main.Players[player].Achievements[3] == false)
                {
                    player.SetData("JobsBuilderQuestCount", 0);
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые деньги", 100, "Заработай на стройке 3 000$. Заработано: " + player.GetData<int>("JobsBuilderQuestCount") + "$ / 3 000$");
                }  
                else if (Main.Players[player].Achievements[3] == true && Main.Players[player].Achievements[4] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые деньги", 100, "Вернитесь к Гарри и поговорите с ним");
                }    
                else if (Main.Players[player].Achievements[4] == true && Main.Players[player].Achievements[5] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые шаги", 100, "Получите права категории Drive D в центре лицензирования");
                }
                else if (Main.Players[player].Achievements[5] == true && Main.Players[player].Achievements[6] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые шаги", 100, "Вернитесь к Гарри и поговорите с ним");
                }
                else if (Main.Players[player].Achievements[6] == true && Main.Players[player].Achievements[7] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые шаги", 100, "Посмотрите свои документы");
                }
                else if (Main.Players[player].Achievements[7] == true && Main.Players[player].Achievements[8] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые шаги", 100, "Поговорите с Гарри");
                }
                else if (Main.Players[player].Achievements[8] == true && Main.Players[player].Achievements[9] == false)
                {
                    Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Путь к богатсву", 100, "Езжайте к Эндрю, другу Гарри, и поговорите с ним");
                }
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"CheckAchievements\":\n" + e.ToString());
            }
        }

        public async Task<bool> Save(Player player)
        {
            try
            {
                bool inveh = NAPI.Player.IsPlayerInAnyVehicle(player);

                Customization.SaveCharacter(player);

                Vector3 LPos = (inveh) ? player.Vehicle.Position + new Vector3(0, 0, 0.5) : player.Position;
                string pos = JsonConvert.SerializeObject(LPos);
                try
                {
                    if (InsideHouseID != -1)
                    {
                        House house = HouseManager.Houses.FirstOrDefault(h => h.ID == InsideHouseID);
                        if (house != null)
                            pos = JsonConvert.SerializeObject(house.Position + new Vector3(0, 0, 1.12));
                    }
                    if (InsideGarageID != -1)
                    {
                        Garage garage = GarageManager.Garages[InsideGarageID];
                        pos = JsonConvert.SerializeObject(garage.Position + new Vector3(0, 0, 1.12));
                    }
                    if (ExteriorPos != new Vector3())
                    {
                        Vector3 position = ExteriorPos;
                        pos = JsonConvert.SerializeObject(position + new Vector3(0, 0, 1.12));
                    }
                    if (InsideHotelID != -1)
                    {
                        Vector3 position = Houses.Hotel.HotelEnters[InsideHotelID];
                        pos = JsonConvert.SerializeObject(position + new Vector3(0, 0, 1.12));
                    }
                    if (TuningShop != -1)
                    {
                        Vector3 position = BusinessManager.BizList[TuningShop].EnterPoint;
                        pos = JsonConvert.SerializeObject(position + new Vector3(0, 0, 1.12));
                    }
                    if (EnterCinema != -1)
                    {
                        Vector3 position = player.GetData<Vector3>("CinemaNumPosition");
                        pos = JsonConvert.SerializeObject(position + new Vector3(0, 0, 1.12));
                    }
                }
                catch (Exception e) { Log.Write("EXCEPTION AT \"UnLoadPos\":\n" + e.ToString()); }

                try
                {
                    if (IsSpawned && !IsAlive)
                    {
                        pos = JsonConvert.SerializeObject(Fractions.Ems.emsCheckpoints[2]);
                        Health = 20;
                        Armor = 0;
                    }
                    else
                    {
                        Health = player.Health;
                        Armor = player.Armor;
                    }
                }
                catch (Exception e) { Log.Write("EXCEPTION AT \"UnLoadHP\":\n" + e.ToString()); }

                try
                {
                    var aItem = nInventory.Find(UUID, ItemType.BodyArmor);
                    if (aItem != null && aItem.IsActive)
                        aItem.Data = $"{Armor}";
                }
                catch (Exception e) { Log.Write("EXCEPTION AT \"UnLoadArmorItem\":\n" + e.ToString()); }

                try
                {
                    var all_vehicles = VehicleManager.getAllPlayerVehicles($"{FirstName}_{LastName}");
                    foreach (var number in all_vehicles)
                        VehicleManager.Save(number);
                }
                catch (Exception e) { Log.Write("EXCEPTION AT \"UnLoadVehicles\":\n" + e.ToString()); }

                if (!IsSpawned)
                    pos = JsonConvert.SerializeObject(SpawnPos);

                Main.PlayerSlotsInfo[UUID] = new Tuple<int, int, int, long, long, int>(LVL, EXP, FractionID, Money, MoneySystem.Bank.Accounts[Bank].Balance, PersonID);

                await MySQL.QueryAsync($"UPDATE `characters` SET `pos`='{pos}',`gender`={Gender},`health`={Health},`armor`={Armor},`lvl`={LVL},`exp`={EXP}," +
                    $"`money`={Money},`bank`={Bank},`work`={WorkID},`fraction`={FractionID},`fractionlvl`={FractionLVL},`arrest`={ArrestTime},`fines`={Fines}," +
                    $"`wanted`='{JsonConvert.SerializeObject(WantedLVL)}',`workstats`='{JsonConvert.SerializeObject(WorksStats)}',`biz`='{JsonConvert.SerializeObject(BizIDs)}',`adminlvl`={AdminLVL}," +
                    $"`licenses`='{JsonConvert.SerializeObject(Licenses)}',`unwarn`='{MySQL.ConvertTime(Unwarn)}',`unmute`='{Unmute}'," +
                    $"`warns`={Warns},`hotel`={HotelID},`hotelleft`={HotelLeft},`lastveh`='{LastVeh}',`onduty`={OnDuty},`lasthour`={LastHourMin},`lastbonus`={LastBonus},`isbonused`={IsBonused}," +
                    $"`luckywheelspins`={LuckyWheelSpins},`demorgan`={DemorganTime},`contacts`='{JsonConvert.SerializeObject(Contacts)}',`achiev`='{JsonConvert.SerializeObject(Achievements)}',`sim`={Sim},`PetName`='{PetName}'," +
                    $"`personid`='{PersonID}',`eat`='{Eat}',`water`='{Water}',`age`='{Age}',`demorganreason`='{DemroganReason}',`demorganadmin`='{DemorganAdmin}' WHERE `uuid`={UUID}");

                MoneySystem.Bank.Save(Bank);
                await Log.DebugAsync($"Player [{FirstName}:{LastName}] was saved.");
                return true;
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"Save\":\n" + e.ToString());
                return false;
            }
        }

        public async Task<int> Create(Player player, string firstName, string lastName)
        {
            try
            {
                if (Main.Players.ContainsKey(player))
                {
                    Log.Debug("Main.Players.ContainsKey(player)", nLog.Type.Error);
                    return -1;
                }

                if (firstName.Length < 1 || lastName.Length < 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Ошибка в длине имени/фамилии", 3000);
                    return -1;
                }
                if (Main.PlayerNames.ContainsValue($"{firstName}_{lastName}"))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Данное имя уже занято", 3000);
                    return -1;
                }

                UUID = GenerateUUID();
                PersonID = GeneratePersonID();

                FirstName = firstName;
                LastName = lastName;

                Bank = MoneySystem.Bank.Create($"{firstName}_{lastName}");

                Main.PlayerBankAccs.Add($"{firstName}_{lastName}", Bank);

                Licenses = new List<bool>() { false, false, false, false, false, false, false, false };

                Achievements = new List<bool>();

                for(uint i = 0; i != 401; i++) Achievements.Add(false);

                SpawnPos = new Vector3(-490.1876, -694.6452, 33.213594);

                Main.PlayerSlotsInfo.Add(UUID, new Tuple<int, int, int, long, long, int>(LVL, EXP, FractionID, Money, MoneySystem.Bank.Accounts[Bank].Balance, PersonID));
                Main.PlayerUUIDs.Add($"{firstName}_{lastName}", UUID);
                Main.PlayerNames.Add(UUID, $"{firstName}_{lastName}");

                await MySQL.QueryAsync($"INSERT INTO `characters`(`uuid`,`personid`,`firstname`,`lastname`,`gender`,`health`,`armor`,`lvl`,`exp`,`money`,`bank`,`work`,`fraction`,`fractionlvl`,`arrest`,`fines`,`demorgan`,`wanted`," +
                    $"`workstats`,`biz`,`adminlvl`,`licenses`,`unwarn`,`unmute`,`warns`,`lastveh`,`onduty`,`lasthour`,`lastbonus`,`isbonused`,`luckywheelspins`,`hotel`,`hotelleft`,`contacts`,`achiev`,`sim`,`pos`,`createdate`,`eat`,`water`,`age`,`demorganreason`,`demorganadmin`,`cooldown`) " +
                    $"VALUES({UUID},'{PersonID}','{FirstName}','{LastName}',{Gender},{Health},{Armor},{LVL},{EXP},{Money},{Bank},{WorkID},{FractionID},{FractionLVL},{ArrestTime},{Fines},{DemorganTime}," +
                    $"'{JsonConvert.SerializeObject(WantedLVL)}','{JsonConvert.SerializeObject(WorksStats)}','{JsonConvert.SerializeObject(BizIDs)}',{AdminLVL},'{JsonConvert.SerializeObject(Licenses)}','{MySQL.ConvertTime(Unwarn)}'," +
                    $"'{Unmute}',{Warns},'{LastVeh}',{OnDuty},{LastHourMin},{LastBonus},{IsBonused},{LuckyWheelSpins},{HotelID},{HotelLeft},'{JsonConvert.SerializeObject(Contacts)}','{JsonConvert.SerializeObject(Achievements)}',{Sim}," +
                    $"'{JsonConvert.SerializeObject(SpawnPos)}','{MySQL.ConvertTime(CreateDate)}','{Eat}','{Water}','{Age}','{DemroganReason}','{DemorganAdmin}','{MySQL.ConvertTime(Cooldown)}')");
                NAPI.Task.Run(() => { player.Name = FirstName + "_" + LastName; });
                nInventory.Check(UUID);
                Main.Players.Add(player, this);

                return UUID;
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"Create\":\n" + e.ToString());
                return -1;
            }
        }

        private int GenerateUUID()
        {
            var result = 333333;
            while (Main.UUIDs.Contains(result))
                result = Rnd.Next(000001, 999999);

            Main.UUIDs.Add(result);
            return result;
        }

        private int GeneratePersonID()
        {
            var result = 1;
            while (Main.PersonIDs.Contains(result))
            {
                result++;
            }
            Main.PersonIDs.Add(result);
            return result;
        }

        public static Dictionary<string, string> toChange = new Dictionary<string, string>();
        private static MySqlCommand nameCommand;

        public Character()
        {
            nameCommand = new MySqlCommand("UPDATE `characters` SET `firstname`=@fn, `lastname`=@ln WHERE `uuid`=@uuid");
        }

        public static async Task changeName(string oldName)
        {
            try
            {
                if (!toChange.ContainsKey(oldName)) return;

                string newName = toChange[oldName];

                //int UUID = Main.PlayerNames.FirstOrDefault(u => u.Value == oldName).Key;
                int Uuid = Main.PlayerUUIDs.GetValueOrDefault(oldName);
                if (Uuid <= 0)
                {
                    await Log.WriteAsync($"Cant'find UUID of player [{oldName}]", nLog.Type.Warn);
                    return;
                }

                string[] split = newName.Split("_");

                Main.PlayerNames[Uuid] = newName;
                Main.PlayerUUIDs.Remove(oldName);
                Main.PlayerUUIDs.Add(newName, Uuid);
                try { 
                    if(Main.PlayerBankAccs.ContainsKey(oldName)) { 
                        int bank = Main.PlayerBankAccs[oldName];
                        Main.PlayerBankAccs.Add(newName, bank);
                        Main.PlayerBankAccs.Remove(oldName);
                    }
                } catch { }

                MySqlCommand cmd = nameCommand;
                cmd.Parameters.AddWithValue("@fn", split[0]);
                cmd.Parameters.AddWithValue("@ln", split[1]);
                cmd.Parameters.AddWithValue("@uuid", Uuid);
                await MySQL.QueryAsync(cmd);

                NAPI.Task.Run(() =>
                {
                    try
                    {
                        VehicleManager.changeOwner(oldName, newName);
                        BusinessManager.changeOwner(oldName, newName);
                        MoneySystem.Bank.changeHolder(oldName, newName);
                        Houses.HouseManager.ChangeOwner(oldName, newName);
                    }
                    catch { }
                });

                await Log.DebugAsync("Nickname has been changed!", nLog.Type.Success);
                toChange.Remove(oldName);
                MoneySystem.Donations.Rename(oldName, newName);
                GameLog.Name(Uuid, oldName, newName);
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"CHANGENAME\":\n" + e.ToString(), nLog.Type.Error);
            }
        }
        #region WorkStats Handler
        public bool AddExpForWork(int value = 1)
        {
            isHaveWorkStatsForThisWork();
            return WorksStats[GetWorkStatsIndex()].AddExp(value);
        }
        public int GetLevelAtThisWork()
        {
            return WorksStats[GetWorkStatsIndex()].Level;
        }
        //имеется ли данные о текущей работе у игрока?
        public bool isHaveWorkStatsForThisWork()
        {
            if (WorksStats == null) WorksStats = new List<WorkStats>();
            if (WorksStats.Contains(WorksStats.Find(x => x.WorkID == this.WorkID)))
            {
                HasTheMaxLevelOfWorkStatsBeenChanged();
                HasTheMaxExpOfWorkStatsBeenChanged();
                return true; //да
            }
            else
            {
                AddNewWorkStatsForTheWorkID();
                return false; //нет
            }
        }
        public int GetWorkStatsIndex()
        {
            if (WorkID == 0) return 0;
            return WorksStats.FindIndex(x => x.WorkID == WorkID);
        }
        //добавить данные о новой работе
        private void AddNewWorkStatsForTheWorkID()
        {
            WorkStats data = new WorkStats(WorkID, 1, 0, 0, Jobs.WorkManager.MaxLevelForThisWork[WorkID], Jobs.WorkManager.MaxExpForThisWork[WorkID]);
            WorksStats.Add(data);
        }
        //изменилось ли значение максимального уровня для работы, который не дает дальше повышать уровень?
        private bool HasTheMaxLevelOfWorkStatsBeenChanged()
        {
            if (WorksStats[GetWorkStatsIndex()]._maxLevel != Jobs.WorkManager.MaxLevelForThisWork[WorkID])
            {
                ChangeMaxLevelWorkStatsForWorkID();
                return true; //да, изменилось
            }
            else
                return false; //нет, не изменилось
        }
        //изменилось ли значение необходимого опыта для повышения уровня у работы?
        private bool HasTheMaxExpOfWorkStatsBeenChanged()
        {
            if (WorksStats[GetWorkStatsIndex()]._expCountForUpLevel != Jobs.WorkManager.MaxExpForThisWork[WorkID])
            {
                ChangeMaxExpWorkStatsForWorkID();
                return true; //да, изменилось
            }
            else
                return false; //нет, не измелось
        }
        private void ChangeMaxLevelWorkStatsForWorkID()
        {
            WorksStats[GetWorkStatsIndex()].ChangeMaxLevel(Jobs.WorkManager.MaxLevelForThisWork[WorkID]);
        }
        private void ChangeMaxExpWorkStatsForWorkID()
        {
            WorksStats[GetWorkStatsIndex()].ChangeNeedExp(Jobs.WorkManager.MaxExpForThisWork[WorkID]);
        }
        #endregion
    }
}
