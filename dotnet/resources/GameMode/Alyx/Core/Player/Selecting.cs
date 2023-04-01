﻿using GTANetworkAPI;
using System;
using Alyx.GUI;
using Alyx.Houses;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using AlyxSDK;

namespace Alyx.Core
{
    class Selecting : Script
    {
        private static nLog Log = new nLog("Selecting");

        [RemoteEvent("server::takesnowgift")]
        public static void TakeSnowMake(Player player)
        {
            Entity entity = player.GetData<Entity>("SELECTEDENTITYSNOW");
            Exter ext = Exteriors.ExtersList.FirstOrDefault(b => b.ID == entity.GetData<int>("IDS"));
            if (ext != null)
            {
                if (ext.UUID == Main.Players[player].UUID)
                {
                    if (ext.Model == "grand_prop_xmas_snowman")
                    {
                        nInventory.Add(player, new nItem(ItemType.Snowman, 1));
                    }
                    if (ext.Model == "grand_prop_xmas_snowman2")
                    {
                        nInventory.Add(player, new nItem(ItemType.Snowman2, 1));
                    }
                    if (ext.Model == "grand_prop_xmas_igloo")
                    {
                        nInventory.Add(player, new nItem(ItemType.Igla, 1));
                    }
                    Exteriors.ExtersList.Remove(ext);
                    if (entity != null)
                        entity.Delete();
                    MySQL.Query($"DELETE FROM `exteriors` WHERE `id`='{ext.ID}'");
                }
            }
        }
        [RemoteEvent("client::breakssnowman")]
        public static void BreakSnowMan(Player player)
        {
            Entity entity = player.GetData<Entity>("SELECTEDENTITYSNOW");
            Exter ext = Exteriors.ExtersList.FirstOrDefault(b => b.ID == entity.GetData<int>("IDS"));
            if (ext != null)
            {
                if (ext.UUID == Main.Players[player].UUID)
                {
                    Notify.Error(player, "Вы не можете сломать свою фигуру, только забрать");
                }
                else
                {
                    nInventory.Add(player, new nItem(ItemType.Snow, 100));
                    Notify.Error(player, "Вы сломали фигуру и получили 100 снега");
                    Exteriors.ExtersList.Remove(ext);
                    if (entity != null)
                        entity.Delete();
                    MySQL.Query($"DELETE FROM `exteriors` WHERE `id`='{ext.ID}'");
                }
            }
        }

        [RemoteEvent("oSelected")]
        public static void objectSelected(Player player, GTANetworkAPI.Object entity)
        {
            try
            {
                // var entity = (GTANetworkAPI.Object)arguments[0]; // error "Object referance not set to an instance of an object"
                if (entity == null || player == null || !Main.Players.ContainsKey(player)) return;
                //  if (entity.GetSharedData<bool>("PICKEDT") == true)
                if (entity.HasData("SNOWMAKE") && entity.GetData<bool>("SNOWMAKE"))
                {
                    Exter ext = Exteriors.ExtersList.FirstOrDefault(b => b.ID == entity.GetData<int>("IDS"));
                    if (ext != null)
                    {
                        Trigger.ClientEvent(player, "client::opensnowmakemenu", ext.Time);
                        player.SetData<Entity>("SELECTEDENTITYSNOW", entity);
                    }
                    return;
                }
                if (player.HasData("PICKEDT") && player.GetData<bool>("PICKEDT") == true) //вот эту дичь ебался долго!!!
                {
                    Commands.SendToAdmins(3, $"!{{#d35400}}[PICKUP-ITEMS-EXPLOIT] {player.Name} ({player.Value}) ");
                    return;
                }
                entity.SetSharedData("PICKEDT", true);
                var objType = entity.GetSharedData<string>("TYPE");
                switch (objType)
                {
                    case "DROPPED":
                        {
                            if (player.HasData("isRemoveObject"))
                            {
                                NAPI.Task.Run(() => {
                                    try
                                    {
                                        NAPI.Entity.DeleteEntity(entity);
                                    }
                                    catch { }
                                });
                                player.ResetData("isRemoveObject");
                                return;
                            }

                            var id = entity.GetData<int>("ID");
                            if (Items.InProcessering.Contains(id))
                            {
                                entity.SetSharedData("PICKEDT", false);
                                return;
                            }
                            Items.InProcessering.Add(id);

                            nItem item = NAPI.Data.GetEntityData(entity, "ITEM");
                            if (item.Type == ItemType.BodyArmor && nInventory.Find(Main.Players[player].UUID, ItemType.BodyArmor) != null)
                            {
                                entity.SetSharedData("PICKEDT", false);
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Недостаточно места в инвентаре", 3000);
                                Items.InProcessering.Remove(id);
                                return;
                            }

                            var tryAdd = nInventory.TryAdd(player, item);
                            if (tryAdd == -1 || (tryAdd > 0 && nInventory.WeaponsItems.Contains(item.Type)))
                            {
                                entity.SetSharedData("PICKEDT", false);
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Недостаточно места в инвентаре", 3000);
                                Items.InProcessering.Remove(id);
                                return;
                            }
                            else if (tryAdd > 0)
                            {
                                entity.SetSharedData("PICKEDT", false);
                                nInventory.Add(player, new nItem(item.Type, item.Count - tryAdd, item.Data));
                                GameLog.Items($"ground", $"player({Main.Players[player].UUID})", Convert.ToInt32(item.Type), item.Count - tryAdd, $"{item.Data}");
                                item.Count = tryAdd;
                                entity.SetData("ITEM", item);
                                Items.InProcessering.Remove(id);
                            }
                            else
                            {
                                NAPI.Task.Run(() => { try { NAPI.Entity.DeleteEntity(entity); } catch { } });
                                nInventory.Add(player, item);
                                GameLog.Items($"ground", $"player({Main.Players[player].UUID})", Convert.ToInt32(item.Type), item.Count, $"{item.Data}");
                            }
                            Main.OnAntiAnim(player);
                            player.PlayAnimation("random@domestic", "pickup_low", 39);
                            NAPI.Task.Run(() => { try { player.StopAnimation(); Main.OffAntiAnim(player); } catch { } }, 1700);
                            return;
                        }
                    case "WeaponSafe":
                    case "SubjectSafe":
                    case "ClothesSafe":
                        {
                            entity.SetSharedData("PICKEDT", false);
                            if (Main.Players[player].InsideHouseID == -1) return;
                            int houseID = Main.Players[player].InsideHouseID;
                            House house = HouseManager.Houses.FirstOrDefault(h => h.ID == Main.Players[player].InsideHouseID);
                            if (house == null) return;
                            if (!house.Owner.Equals(player.Name))
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Пользоваться мебелью может только владелец дома.", 3000);
                                return;
                            }
                            var furnID = NAPI.Data.GetEntityData(entity, "ID");
                            HouseFurniture furniture = FurnitureManager.HouseFurnitures[houseID][furnID];
                            var items = FurnitureManager.FurnituresItems[houseID][furnID];
                            if (items == null) return;
                            player.SetData("OpennedSafe", furnID);
                            player.SetData("OPENOUT_TYPE", FurnitureManager.SafesType[furniture.Name]);
                            Dashboard.OpenOut(player, items, furniture.Name, FurnitureManager.SafesType[furniture.Name]);
                            return;
                        }
                    case "MoneyBag":
                        {
                            if (player.HasData("HEIST_DRILL") || NAPI.Data.HasEntityData(player, "HAND_MONEY"))
                            {
                                entity.SetSharedData("PICKEDT", false);
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть сумка", 3000);
                                return;
                            }

                            var money = NAPI.Data.GetEntityData(entity, "MONEY_IN_BAG");

                            player.SetClothes(5, 45, 0);
                            var item = new nItem(ItemType.BagWithMoney, 1, $"{money}");
                            nInventory.Items[Main.Players[player].UUID].Add(item);
                            Dashboard.sendItems(player);
                            player.SetData("HAND_MONEY", true);
                            NAPI.Task.Run(() => { try { NAPI.Entity.DeleteEntity(entity); } catch { } });
                            Main.OnAntiAnim(player);
                            player.PlayAnimation("random@domestic", "pickup_low", 39);
                            NAPI.Task.Run(() => { try { player.StopAnimation(); Main.OffAntiAnim(player); } catch { } }, 1700);
                            return;
                        }
                    case "DrillBag":
                        {
                            if (player.HasData("HEIST_DRILL") || player.HasData("HAND_MONEY"))
                            {
                                entity.SetSharedData("PICKEDT", false);
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть дрель или деньги в руках", 3000);
                                return;
                            }

                            player.SetClothes(5, 41, 0);
                            nInventory.Add(player, new nItem(ItemType.BagWithDrill));
                            player.SetData("HEIST_DRILL", true);

                            NAPI.Task.Run(() => { try { NAPI.Entity.DeleteEntity(entity); } catch { } });
                            Main.OnAntiAnim(player);
                            player.PlayAnimation("random@domestic", "pickup_low", 39);
                            NAPI.Task.Run(() => { try { player.StopAnimation(); Main.OffAntiAnim(player); } catch { } }, 1700);
                            return;
                        }
                }
            }
            catch (Exception e) { Log.Write($"oSelected/: {e.ToString()}\n{e.StackTrace}", nLog.Type.Error); }
        }

        [RemoteEvent("vehicleSelected")]
        public static void vehicleSelected(Player player, params object[] arguments)
        {
            try
            {
                var vehicle = (Vehicle)arguments[0];
                int index = (int)arguments[1];
                if (vehicle == null || player.Position.DistanceTo(vehicle.Position) > 5)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Машина находится далеко от Вас", 3000);
                    return;
                }
                if (vehicle.HasSharedData("ACCESS") && vehicle.GetSharedData<string>("ACCESS") == "DUMMY") return;
                Vector3 BonePos = new Vector3((float)arguments[2], (float)arguments[3], (float)arguments[4]);
                switch (index)
                {
                    case 0:
                        if (player.IsInVehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете открыть/закрыть капот, находясь в машине", 3000);
                            return;
                        }
                        if (player.Position.DistanceTo(BonePos) > 2)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы слишком далеко от капота", 3000);
                            return;
                        }
                        if (VehicleStreaming.GetDoorState(vehicle, DoorID.DoorHood) == DoorState.DoorClosed)
                        {
                            if (VehicleStreaming.GetLockState(vehicle))
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете открыть капот, пока машина закрыта", 3000);
                                return;
                            }
                            VehicleStreaming.SetDoorState(vehicle, DoorID.DoorHood, DoorState.DoorOpen);
                        }
                        else VehicleStreaming.SetDoorState(vehicle, DoorID.DoorHood, DoorState.DoorClosed);
                        return;
                    case 1:
                        if (player.IsInVehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете открыть/закрыть багажник, находясь в машине", 3000);
                            return;
                        }
                        if (player.Position.DistanceTo(BonePos) > 2)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы слишком далеко от багажника", 3000);
                            return;
                        }
                        if (VehicleStreaming.GetDoorState(vehicle, DoorID.DoorTrunk) == DoorState.DoorOpen)
                        {
                            Commands.RPChat("me", player, $"закрыл(а) багажник");
                            VehicleStreaming.SetDoorState(vehicle, DoorID.DoorTrunk, DoorState.DoorClosed);
                            foreach (var p in Main.Players.Keys.ToList())
                            {
                                if (p == null || !Main.Players.ContainsKey(p)) continue;
                                if (p.HasData("OPENOUT_TYPE") && p.GetData<int>("OPENOUT_TYPE") == 2 && p.HasData("SELECTEDVEH") && p.GetData<Vehicle>("SELECTEDVEH") == vehicle) GUI.Dashboard.Close(p);
                            }
                        }
                        else
                        {
                            if (vehicle.HasData("ACCESS") && (vehicle.GetData<string>("ACCESS") == "PERSONAL" || vehicle.GetData<string>("ACCESS") == "GARAGE"))
                            {
                                var access = VehicleManager.canAccessByNumber(player, vehicle.NumberPlate);
                                if (!access)
                                {
                                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас нет ключей от этого транспорта", 3000);
                                    return;
                                }
                            }
                            if (vehicle.HasData("ACCESS") && vehicle.GetData<string>("ACCESS") == "FRACTION" && vehicle.GetData<int>("FRACTION") != Main.Players[player].FractionID)
                            {
                                if (Main.Players[player].FractionID != 7 && Main.Players[player].FractionID != 9)
                                {
                                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете открыть багажник у этой машины", 3000);
                                    return;
                                }
                            }
                            if (player.Position.DistanceTo(BonePos) > 2)
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы слишком далеко от багажника", 3000);
                                return;
                            }
                            VehicleStreaming.SetDoorState(vehicle, DoorID.DoorTrunk, DoorState.DoorOpen);
                            Commands.RPChat("me", player, $"открыл(а) багажник");
                        }
                        return;
                    case 2:
                        VehicleManager.ChangeVehicleDoors(player, vehicle);
                        return;
                    case 3:
                        if (player.IsInVehicle)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете открыть инвентарь, находясь в машине", 3000);
                            return;
                        }
                        if (NAPI.Data.GetEntityData(vehicle, "ACCESS") == "WORK" || vehicle.Class == 13 || vehicle.Class == 8)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Эта транспортное средство не поддерживает инвентарь", 3000);
                            return;
                        }
                        if (player.Position.DistanceTo(BonePos) > 2)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы слишком далеко от багажника", 3000);
                            return;
                        }
                        if (Main.Players[player].AdminLVL == 0 && VehicleStreaming.GetDoorState(vehicle, DoorID.DoorTrunk) == DoorState.DoorClosed)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете открыть инвентарь машины, пока багажник закрыт", 3000);
                            return;
                        }
                        if (vehicle.GetData<bool>("BAGINUSE") == true)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Дождитесь, пока другой человек перестанет пользоваться багажником.", 3000);
                            return;
                        }
                        vehicle.SetData("BAGINUSE", true);
                        GUI.Dashboard.OpenOut(player, vehicle.GetData<List<nItem>>("ITEMS"), "Багажник", 2);
                        player.SetData("SELECTEDVEH", vehicle);
                        return;
                    case 4:
                        var itempetrol = nInventory.Find(Main.Players[player].UUID, ItemType.GasCan);
                        if (itempetrol == null)
                        {
                            Notify.Error(player, "У вас нет канистры");
                            return;
                        }
                        if (!itempetrol.IsActive)
                        {
                            Notify.Error(player, "У вас нет канистры в руках");
                            return;
                        }
                        if (!vehicle.HasSharedData("PETROL"))
                        {
                            Notify.Error(player, "Данный транспорт нельзя заправить");
                            return;
                        }
                        var fuel = vehicle.GetSharedData<int>("PETROL");
                        if (fuel == VehicleManager.VehicleTank[vehicle.Class])
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"В машине полный бак", 3000);
                            GUI.Dashboard.Close(player);
                            return;
                        }
                        fuel += 20;
                        Notify.Succ(player, $"Вы заправили транспорт на 20л, теперь в баке {fuel}");
                        nInventory.Remove(player, (new nItem(ItemType.GasCan, 1)));
                        BasicSync.DetachObject(player);
                        if (fuel > VehicleManager.VehicleTank[vehicle.Class]) fuel = VehicleManager.VehicleTank[vehicle.Class];
                        vehicle.SetSharedData("PETROL", fuel);
                        if (vehicle.HasData("ACCESS") && vehicle.GetData<string>("ACCESS") == "GARAGE")
                        {
                            var number = vehicle.NumberPlate;
                            VehicleManager.Vehicles[number].Fuel = fuel;
                        }
                        return;
                    case 5:
                        if (player.Position.DistanceTo(BonePos) > 2)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы слишком далеко от багажника", 3000);
                            return;
                        }

                        VehicleHandlers.Trunk.Enter(vehicle, player);

                        return;
                }

            }
            catch (Exception e) { Log.Write("vSelected: " + e.Message, nLog.Type.Error); }

        }
        [RemoteEvent("server::toservbtnPopUp")]
        public static void ToServerBtnPopUp(Player player, int id)
        {
            try
            {
                switch(id)
                {
                    case 0:
                        if (player.IsInVehicle) return;
                        {
                            var acc = Main.Players[player];
                            string gender = (acc.Gender) ? "Мужской" : "Женский";
                            string fraction = (acc.FractionID > 0) ? Fractions.Manager.FractionNames[acc.FractionID] : "Нет";
                            string work = (acc.WorkID > 0) ? Jobs.WorkManager.JobStats[acc.WorkID - 1] : "Безработный";
                            List<object> data = new List<object>
                            {
                                acc.UUID,
                                acc.FirstName,
                                acc.LastName,
                                acc.CreateDate.ToString("dd.MM.yyyy"),
                                gender,
                                fraction,
                                work
                            };
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                            Trigger.ClientEvent(player, "passport", json);
                            Trigger.ClientEvent(player, "newPassport", player, acc.UUID);
                            if (Main.Players[player].Achievements[6] == true && Main.Players[player].Achievements[7] == false)
                            {
                                Main.Players[player].Achievements[7] = true;
                                Trigger.ClientEvent(player, "client::addToMissionsOnHud", true, "Первые шаги", 100, "Поговорите с Гарри");
                            }
                        }
                        return;
                    case 1:
                        if (player.IsInVehicle) return;
                        {
                            var acc = Main.Players[player];
                            string gender = (acc.Gender) ? "Мужской" : "Женский";

                            var lic = "";
                            for (int i = 0; i < acc.Licenses.Count; i++)
                                if (acc.Licenses[i]) lic += $"{Main.LicWords[i]} / ";
                            if (lic == "") lic = "Отсутствуют";

                            List<string> data = new List<string>
                            {
                                acc.FirstName,
                                acc.LastName,
                                acc.CreateDate.ToString("dd.MM.yyyy"),
                                gender,
                                lic
                            };

                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                            Trigger.ClientEvent(player, "licenses", json);
                        }
                        return;
                    case 2:
                        if (player.IsInVehicle) return;
                        Trigger.ClientEvent(player, "client::openplayermenu");
                        return;
                }
            }
            catch (Exception e) { Log.Write($"PopUp: " + e.ToString(), nLog.Type.Error); }
        } 
        [RemoteEvent("server::toservbtnAltPopUp")]
        public static void toservbtnAltPopUp(Player player, int id)
        {
            try
            {
                switch(id)
                {
                    case 1:
                        if (player.IsInVehicle) return;
                        if (player.HasSharedData("BOOMBOXON") && !player.GetSharedData<bool>("BOOMBOXON"))
                        {
                            var item = nInventory.Find(Main.Players[player].UUID, ItemType.Boombox);
                            if (item == null)
                            {
                                Notify.Error(player, "У вас нет бумбокса");
                                return;
                            }
                            XMR.CMD_Boombox(player);
                        }
                        else if (player.HasSharedData("BOOMBOXON") && player.GetSharedData<bool>("BOOMBOXON"))
                        {
                            if (player.Position.DistanceTo(player.GetData<Vector3>("BOOMBOXONPOSITION")) > 2)
                            {
                                Notify.Error(player, "Бумбокс далеко от вас");
                                return;
                            }
                            XMR.remove_boom_box(player);
                        }
                        return;
                    case 2:
                        if (player.IsInVehicle) return;
                        if (player.Position.DistanceTo(player.GetData<Vector3>("BOOMBOXONPOSITION")) > 2)
                        {
                            Notify.Error(player, "Бумбокс далеко от вас");
                            return;
                        } 
                        XMR.CMD_setstation(player);
                        return;
                }
            }
            catch (Exception e) { Log.Write($"PopUpOnAlt: " + e.ToString(), nLog.Type.Error); }
        }


        [RemoteEvent("pSelected")]
        public static void playerSelected(Player player, params object[] arguments)
        {
            try
            {
                if (player.HasData("ARENA")) return;
                var target = (Player)arguments[0];
                if (target == null || player.Position.DistanceTo(target.Position) > 2)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Игрок находится далеко от Вас", 3000);
                    return;
                }
                player.SetData("SELECTEDPLAYER", target);

                if (arguments.Length == 1) return;
                var action = arguments[1].ToString();
                switch (action)
                {
                    case "Пожать руку":
                        if (player.IsInVehicle) return;
                        playerHandshakeTarget(player, target);
                        return;
                    case "Вести за собой":
                        if (player.IsInVehicle) return;
                        Fractions.FractionCommands.targetFollowPlayer(player, target);
                        return;
                    case "Ограбить":
                        if (player.IsInVehicle) return;
                        Fractions.FractionCommands.robberyTarget(player, target);
                        return;
                    case "Поцеловать":
                        if (player.IsInVehicle) return;
                        playerKissTarget(player, target);
                        return;
                    case "Взять на руки":
                        if (player.IsInVehicle) return;
                        target.SetData("SELECTEDPLAYERCARRY", player);
                        Trigger.ClientEvent(target, "openDialog", "SUCCES_CARRY", $"{player.Name.Replace('_', ' ')}({player.Value}) предложил вам взять вас на руки, вы соглсаны?");
                        return;
                    case "Отпустить":
                        if (player.IsInVehicle) return;
                        if (!target.HasData("FOLLOWING"))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Этого игрока никто не тащит", 3000);
                            return;
                        }
                        if (!player.HasData("FOLLOWER") || player.GetData<Player>("FOLLOWER") != target)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Этого игрока тащит кто-то другой", 3000);
                            return;
                        }
                        Fractions.FractionCommands.unFollow(player, target);
                        return;
                    case "Вытащить из багажника":

                        Vehicle vehicle = VehicleHandlers.Trunk.Get(target);

                        if (vehicle == null)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок не находится в багажнике", 3000);
                            return;
                        }
                        if (VehicleStreaming.GetDoorState(vehicle, DoorID.DoorTrunk) == DoorState.DoorClosed)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Багажник закрыт", 3000);
                            return;
                        }

                        VehicleHandlers.Trunk.Reset(vehicle);
                        Notify.Send(target, NotifyType.Success, NotifyPosition.BottomCenter, "Вас высунули из багажника", 3000);
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы высунули игрока ({target.Value}) из багажника", 3000);

                        Commands.RPChat("me", player, $"вытащил(а) из багажника гражданина ({target.Value})");

                        return;
                    case "Запихнуть в багажник":
                        if (!target.GetData<bool>("CUFFED"))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок не в наручниках или стяжках", 3000);
                            return;
                        }

                        vehicle = null;
                        float distance = 999999;

                        foreach (Vehicle veh in NAPI.Pools.GetAllVehicles())
                        {
                            float my = veh.Position.DistanceTo(player.Position);
                            if (my < 10 && distance > my)
                            {
                                vehicle = veh;
                                distance = my;
                            }
                        }

                        if (vehicle == null)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Рядом нет никакого транспорта", 3000);
                            return;
                        }
                        if (VehicleStreaming.GetDoorState(vehicle, DoorID.DoorTrunk) == DoorState.DoorClosed)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Багажник закрыт", 3000);
                            return;
                        }
                        if (VehicleHandlers.Trunk.Get(vehicle) != null)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "В багажнике кто-то уже находится!", 3000);
                            return;
                        }
                        if (VehicleHandlers.Trunk.Get(target) != null)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Игрок уже находится в багажнике!", 3000);
                            return;
                        }

                        VehicleHandlers.Trunk.Set(vehicle, player);
                        Notify.Send(target, NotifyType.Error, NotifyPosition.BottomCenter, "Вас засунули в багажник!", 3000);
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы засунули игрока ({target.Value}) в багажник", 3000);

                        Commands.RPChat("me", player, $"засунул(а) в багажник гражданина ({target.Value})");

                        return;
                    case "Обыскать":
                        if (player.IsInVehicle) return;
                        {
                            if (!target.GetData<bool>("CUFFED"))
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок не в наручниках", 3000);
                                return;
                            }

                            var items = nInventory.Items[Main.Players[target].UUID];
                            List<string> itemNames = new List<string>();
                            List<string> weapons = new List<string>();
                            foreach (var i in items)
                            {
                                if (nInventory.ClothesItems.Contains(i.Type)) continue;
                                if (nInventory.WeaponsItems.Contains(i.Type))
                                    weapons.Add($"{nInventory.ItemsNames[(int)i.Type]} {i.Data}");
                                else
                                    itemNames.Add($"{nInventory.ItemsNames[(int)i.Type]} x{i.Count}");
                            }

                            var data = new SearchObject();
                            data.Name = target.Name.Replace('_', ' ');
                            data.Weapons = weapons;
                            data.Items = itemNames;

                            Trigger.ClientEvent(player, "newPassport", target, Main.Players[target].UUID);
                            Trigger.ClientEvent(player, "bsearchOpen", JsonConvert.SerializeObject(data));
                            return;
                        }
                    case "Посмотреть паспорт":
                        if (player.IsInVehicle) return;
                        {
                            if (!target.GetData<bool>("CUFFED"))
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок не в наручниках", 3000);
                                return;
                            }

                            var acc = Main.Players[target];
                            string gender = (acc.Gender) ? "Мужской" : "Женский";
                            string fraction = (acc.FractionID > 0) ? Fractions.Manager.FractionNames[acc.FractionID] : "Нет";
                            string work = (acc.WorkID > 0) ? Jobs.WorkManager.JobStats[acc.WorkID] : "Безработный";
                            List<object> data = new List<object>
                            {
                                acc.UUID,
                                acc.FirstName,
                                acc.LastName,
                                acc.CreateDate.ToString("dd.MM.yyyy"),
                                gender,
                                fraction,
                                work
                            };
                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                            Trigger.ClientEvent(player, "passport", json);
                            Trigger.ClientEvent(player, "newPassport", target, acc.UUID);
                        }
                        return;
                    case "Посмотреть лицензии":
                        if (player.IsInVehicle) return;
                        {
                            if (!target.GetData<bool>("CUFFED"))
                            {
                                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок не в наручниках", 3000);
                                return;
                            }

                            var acc = Main.Players[target];
                            string gender = (acc.Gender) ? "Мужской" : "Женский";

                            var lic = "";
                            for (int i = 0; i < acc.Licenses.Count; i++)
                                if (acc.Licenses[i]) lic += $"{Main.LicWords[i]} / ";
                            if (lic == "") lic = "Отсутствуют";

                            List<string> data = new List<string>
                            {
                                acc.FirstName,
                                acc.LastName,
                                acc.CreateDate.ToString("dd.MM.yyyy"),
                                gender,
                                lic
                            };

                            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                            Trigger.ClientEvent(player, "licenses", json);
                        }
                        return;
                    case "Изъять оружие":
                        if (player.IsInVehicle) return;
                        playerTakeGuns(player, target);
                        return;
                    case "Изъять нелегал":
                        if (player.IsInVehicle) return;
                        playerTakeIlleagal(player, target);
                        return;
                    case "Продать аптечку":
                        if (player.IsInVehicle) return;
                        Trigger.ClientEvent(player, "openInput", "Продать аптечку", "Цена", 4, "player_medkit");
                        return;
                    case "Предложить лечение":
                        if (player.IsInVehicle) return;
                        Trigger.ClientEvent(player, "openInput", "Предложить лечение", "Цена", 4, "player_heal");
                        return;
                    case "Выдать медицинскую карту":
                        if (player.IsInVehicle) return;
                        if (Main.Players[player].FractionLVL < 5)
                        {
                            Notify.Error(player, "Вы не можете выдавать лицензии");
                            return;
                        }
                        if (Main.Players[target].Licenses[7])
                        {
                            Notify.Send(target, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть медицинская карта.", 3000);
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Игрока({target.Value}) уже есть медицинская карта", 3000);
                            return;
                        }
                        Notify.Send(target, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы получили медицинскую карту", 3000);
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы выдали медицинскую карту Игроку ({target.Value})", 3000);
                        Main.Players[target].Licenses[7] = true;
                        GUI.Dashboard.sendStats(player);
                        return;
                    case "Выдать лицензию на оружие":
                        if (player.IsInVehicle) return;
                        if (Main.Players[player].FractionLVL < 5)
                        {
                            Notify.Error(player, "Вы не можете выдавать лицензии");
                            return;
                        }
                        if (Main.Players[target].Licenses[6])
                        {
                            Notify.Send(target, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть лицензия на оружие", 3000);
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Игрока({target.Value}) уже есть лицензия на оружие", 3000);
                            return;
                        }
                        Notify.Send(target, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы получили лицензию на оружие", 3000);
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы выдали лицензию на оружие Игроку ({target.Value})", 3000);
                        Main.Players[target].Licenses[6] = true;
                        GUI.Dashboard.sendStats(player);
                        return;
                    case "Вылечить":
                        if (player.IsInVehicle) return;
                        playerHealTarget(player, target);
                        return;
                    case "Продать машину":
                        VehicleManager.sellCar(player, target);
                        return;
                    case "Продать дом":
                        House house = HouseManager.GetHouse(player, true);
                        if (house == null)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас нет дома", 3000);
                            return;
                        }
                        Trigger.ClientEvent(player, "openInput", "Продать дом", "Цена $$$", 8, "player_offerhousesell");
                        return;
                    case "Заселить в дом":
                        HouseManager.InviteToRoom(player, target);
                        return;
                    case "Пригласить в дом":
                        HouseManager.InvitePlayerToHouse(player, target);
                        return;
                    case "Передать деньги":
                        if (Main.Players[player].LVL < 1)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Перевод денег доступен после первого уровня", 3000);
                            return;
                        }
                        Trigger.ClientEvent(player, "openInput", "Передать деньги", "Сумма $$$", 4, "player_givemoney");
                        return;
                    case "Предложить обмен":
                        target.SetData("OFFER_MAKER", player);
                        target.SetData("REQUEST", "OFFER_ITEMS");
                        target.SetData("IS_REQUESTED", true);
                        Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) предложил Вам обменяться предметами. Y/N - принять/отклонить", 3000);
                        Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы предложили игроку ({target.Value}) обменяться предметами.", 3000);
                        return;
                    case "Мешок":
                        if (player.IsInVehicle) return;
                        Fractions.FractionCommands.playerChangePocket(player, target);
                        return;
                    case "Сорвать маску":
                        if (player.IsInVehicle) return;
                        Fractions.FractionCommands.playerTakeoffMask(player, target);
                        return;
                    case "Выписать штраф":
                        if (player.IsInVehicle) return;
                        player.SetData("TICKETTARGET", target);
                        Trigger.ClientEvent(player, "openInput", "Выписать штраф (сумма)", "Сумма от 0 до 7000$", 9, "player_ticketsum");
                        return;
                }
            }
            catch (Exception e) { Log.Write($"pSelected: " + e.ToString(), nLog.Type.Error); }
        }

        public static void playerKissTarget(Player player, Player target)
        {
            if ((!player.HasData("CUFFED") && !player.HasSharedData("InDeath")) || player.HasData("CUFFED") && player.GetData<bool>("CUFFED") == false && player.HasSharedData("InDeath") && player.GetSharedData<bool>("InDeath") == false)
            {
                if ((!target.HasData("CUFFED") && !target.HasSharedData("InDeath")) || target.HasData("CUFFED") && target.GetData<bool>("CUFFED") == false && target.HasSharedData("InDeath") && target.GetSharedData<bool>("InDeath") == false)
                {
                    target.SetData("KISS", player);
                    target.SetData("REQUEST", "KISS");
                    target.SetData("IS_REQUESTED", true);
                    Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, $"({player.Value}) хочет поцеловать Вас. Y/N - согласиться/отказаться", 3000);
                    //Main.OpenAnswer(target, $"Гражданин ({player.Name}) хочет поцеловать Вас");
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы предложили ({target.Value}) поцеловаться.", 3000);
                }
                else Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Не хочет целоваться", 3000);
            }
            else Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Не может поцеловаться", 3000);
        }

        public static void kissTarget(Player player)
        {
            if (!Main.Players.ContainsKey(player) || !player.HasData("KISS") || !Main.Players.ContainsKey(player.GetData<Player>("KISS"))) return;
            Player target = player.GetData<Player>("KISS");
            if ((!player.HasData("CUFFED") && !player.HasSharedData("InDeath")) || player.HasData("CUFFED") && player.GetData<bool>("CUFFED") == false && player.HasSharedData("InDeath") && player.GetSharedData<bool>("InDeath") == false)
            {
                if ((!target.HasData("CUFFED") && !target.HasSharedData("InDeath")) || target.HasData("CUFFED") && target.GetData<bool>("CUFFED") == false && target.HasSharedData("InDeath") && target.GetSharedData<bool>("InDeath") == false)
                {
                    player.PlayAnimation("mp_ped_interaction", "kisses_guy_a", 39);
                    target.PlayAnimation("mp_ped_interaction", "kisses_guy_b", 39);

                    //Trigger.PlayerEvent(player, "newFriend", target);
                    //Trigger.PlayerEvent(target, "newFriend", player);

                    Main.OnAntiAnim(player);
                    Main.OnAntiAnim(target);

                    NAPI.Task.Run(() => { try { Main.OffAntiAnim(player); Main.OffAntiAnim(target); player.PlayAnimation("rcmcollect_paperleadinout@", "kneeling_arrest_get_up", 33); target.PlayAnimation("rcmcollect_paperleadinout@", "kneeling_arrest_get_up", 33); } catch { } }, 4500);
                }
            }
        }
        [RemoteEvent("changestatecarry")]
        public static void SuccesExitCarry(Player player)
        {
            Player Target = player.GetData<Player>("SELECTEDPLAYERCARRY");
            SuccesCarry(Target, player);
        }
        public static void SuccesCarry(Player Player, Player Target)
        {
            //Player Target = player.GetData<Player>("SELECTEDPLAYERCARRY");
            if (Target != null)
            {
                if (Player.HasData("carrystate") && Player.GetData<bool>("carrystate") == true)
                {
                    Player.ResetSharedData("carry");
                    Player.SetData("carrystate", false);
                    Player.SetSharedData("carrystate", false);
                    Player.StopAnimation();
                    Target.StopAnimation();
                    Target.Transparency = 255;
                    Target.SetSharedData("carryinstate", false);
                    Target.SetSharedData("carryplayerid", -1);
                }
                else
                {
                    Player.PlayAnimation("missfinale_c2mcs_1", "fin_c2_mcs_1_camman", 49);
                    Player.SetSharedData("carry", Target.Id);
                    Player.SetData("carrystate", true);
                    Player.SetSharedData("carrystate", true);
                    Target.PlayAnimation("nm", "firemans_carry", 33);
                    Target.Transparency = 0;
                    Target.SetSharedData("carryinstate", true);
                    Target.SetSharedData("carryplayerid", Target.Value);
                }
            }
        }
        public static void FalseCarry(Player player)
        {
            Player target = player.GetData<Player>("SELECTEDPLAYERCARRY");
            Notify.Error(target, $"{player.Name} отказался от предложения");
            return;
        }
        public static void playerTransferMoney(Player player, string arg)
        {
            if (Main.Players[player].LVL < 1)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Передача денег будет доступна начиная с 1 уровня.", 3000);
                return;
            }
            try
            {
                Convert.ToInt32(arg);
            }
            catch
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Введите корректные данные", 3000);
                return;
            }
            var amount = Convert.ToInt32(arg);
            if (amount < 1)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Введите корректные данные", 3000);
                return;
            }
            if (amount > 5000)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы не можете передовать более 5 000$", 3000);
                return;
            }
            Player target = player.GetData<Player>("SELECTEDPLAYER");
            if (!Main.Players.ContainsKey(target) || player.Position.DistanceTo(target.Position) > 2)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок слишком далеко от Вас", 3000);
                return;
            }
            if (amount > Main.Players[player].Money)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас недостаточно средств", 3000);
                return;
            }

            player.SetData("NEXT_TRANSFERM", DateTime.Now.AddSeconds(3));
            player.PlayAnimation("mp_common", "givetake2_a", 1);
            NAPI.Task.Run(() =>
            {
                player.StopAnimation();
            }, 2000);
            Notify.Send(target, NotifyType.Info, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) передал Вам {amount}$", 3000);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы передали игроку ({target.Value}) {amount}$", 3000);
            MoneySystem.Wallet.Change(target, amount);
            MoneySystem.Wallet.Change(player, -amount);
            GameLog.Money($"player({Main.Players[player].UUID})", $"player({Main.Players[target].UUID})", amount, $"transfer");
            Commands.RPChat("me", player, $"передал(а) {amount}$ " + "{name}", target);
        }
        public static void playerHealTarget(Player player, Player target)
        {
            try
            {
                if (player.Position.DistanceTo(target.Position) > 2)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок слишком далеко от Вас", 3000);
                    return;
                }
                var item = nInventory.Find(Main.Players[player].UUID, ItemType.HealthKit);
                if (item == null || item.Count < 1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас нет аптечки", 3000);
                    return;
                }

                nInventory.Remove(player, ItemType.HealthKit, 1);
                if (target.HasData("IS_DYING"))
                {
                    MenuManager.Close(player);
                    Main.OnAntiAnim(player);
                    player.StopAnimation();
                    player.PlayAnimation("amb@medic@standing@tendtodead@idle_a", "idle_a", 39);
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы начали реанимирование игрока ({target.Value})", 3000);
                    Notify.Send(target, NotifyType.Info, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) начал реанимировать Вас", 3000);
                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            Main.OffAntiAnim(player);
                            player.StopAnimation();
                            NAPI.Entity.SetEntityPosition(player, player.Position + new Vector3(0, 0, 0.5));

                            if (Main.Players[player].FractionID != 8)
                            {
                                var random = new Random();
                                if (random.Next(0, 11) <= 5)
                                {
                                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Игрок ({target.Value}) чуть ласты не склеил. У Вас не вышло его реанимировать", 3000);
                                    return;
                                }
                            }
                            else
                            {
                                if (!target.HasData("NEXT_DEATH_MONEY") || DateTime.Now > target.GetData<DateTime>("NEXT_DEATH_MONEY"))
                                {
                                    MoneySystem.Wallet.Change(player, 150);
                                    GameLog.Money($"server", $"player({Main.Players[player].UUID})", 150, $"revieve({Main.Players[target].UUID})");
                                    target.SetData("NEXT_DEATH_MONEY", DateTime.Now.AddMinutes(15));
                                }
                            }

                            target.StopAnimation();
                            NAPI.Entity.SetEntityPosition(target, target.Position + new Vector3(0, 0, 0.5));
                            target.SetSharedData("InDeath", false);
                            Trigger.ClientEvent(target, "DeathTimer", false);
                            target.Health = 50;
                            target.ResetData("IS_DYING");
                            target.ResetSharedData("IS_DYING");
                            Main.Players[target].IsAlive = true;
                            Main.OffAntiAnim(target);
                            if (target.HasData("DYING_TIMER"))
                            {
                                //Main.StopT(target.GetData<string>("DYING_TIMER"), "timer_18");
                                Timers.Stop(target.GetData<string>("DYING_TIMER"));
                                target.ResetData("DYING_TIMER");
                            }
                            Notify.Send(target, NotifyType.Info, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) реанимировал Вас", 3000);
                            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы реанимировали игрока ({target.Value})", 3000);

                            if (target.HasData("CALLEMS_BLIP"))
                            {
                                NAPI.Entity.DeleteEntity(target.GetData<Blip>("CALLEMS_BLIP"));
                            }
                            if (target.HasData("CALLEMS_COL"))
                            {
                                NAPI.ColShape.DeleteColShape(target.GetData<ColShape>("CALLEMS_COL"));
                            }
                        }
                        catch (Exception e) { Log.Write("playerHealedtarget: " + e.Message, nLog.Type.Error); }
                    }, 15000);
                }
                else
                {
                    Notify.Send(target, NotifyType.Info, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) вылечил Вас с помощью аптечки", 3000);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы вылечили игрока ({target.Value}) с помощью аптечки", 3000);
                    target.Health = 100;
                }
                return;
            }
            catch (Exception e) { Log.Write("playerHealTarget: " + e.Message); }
        }
        public static void playerTakeGuns(Player player, Player target)
        {
            if (player.Position.DistanceTo(target.Position) > 2)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок слишком далеко от Вас", 3000);
                return;
            }
            if (!Fractions.Manager.canUseCommand(player, "takeguns")) return;
            Weapons.RemoveAll(target, true);
            Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) изъял у Вас всё оружие", 3000);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы изъяли всё оружие у игрока ({target.Value})", 3000);
            return;
        }
        public static void playerTakeIlleagal(Player player, Player target)
        {
            if (player.Position.DistanceTo(target.Position) > 2)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок слишком далеко от Вас", 3000);
                return;
            }
            var matItem = nInventory.Find(Main.Players[target].UUID, ItemType.Material);
            var drugItem = nInventory.Find(Main.Players[target].UUID, ItemType.Drugs);
            var materials = (matItem == null) ? 0 : matItem.Count;
            var drugs = (drugItem == null) ? 0 : drugItem.Count;
            if (materials < 1 && drugs < 1)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок не имеет ничего запрещённого", 3000);
                return;
            }
            nInventory.Remove(target, ItemType.Material, materials);
            nInventory.Remove(target, ItemType.Drugs, drugs);
            Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) изъял у Вас запрещённые предметы", 3000);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы изъяили у игрока {target.Value} запрещённые предметы", 3000);
            return;
        }
        public static void playerOfferChangeItems(Player player)
        {
            if (!Main.Players.ContainsKey(player) || !player.HasData("OFFER_MAKER") || !Main.Players.ContainsKey(player.GetData<Player>("OFFER_MAKER"))) return;
            Player offerMaker = player.GetData<Player>("OFFER_MAKER");
            if (Main.Players[player].ArrestTime > 0 || Main.Players[offerMaker].ArrestTime > 0)
            {
                player.ResetData("OFFER_MAKER");
                return;
            }
            if (player.Position.DistanceTo(offerMaker.Position) > 2)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Игрок слишком далеко", 3000);
                return;
            }

            player.SetData("CHANGE_WITH", offerMaker);
            offerMaker.SetData("CHANGE_WITH", player);

            GUI.Dashboard.OpenOut(player, new List<nItem>(), offerMaker.Name, 5);
            GUI.Dashboard.OpenOut(offerMaker, new List<nItem>(), player.Name, 5);

            player.ResetData("OFFER_MAKER");
        }
        public static void playerHandshakeTarget(Player player, Player target)
        {
            if ((!player.HasData("CUFFED") && !player.HasSharedData("InDeath")) || player.HasData("CUFFED") && player.GetData<bool>("CUFFED") == false && player.HasSharedData("InDeath") && player.GetSharedData<bool>("InDeath") == false)
            {
                if ((!target.HasData("CUFFED") && !target.HasSharedData("InDeath")) || target.HasData("CUFFED") && target.GetData<bool>("CUFFED") == false && target.HasSharedData("InDeath") && target.GetSharedData<bool>("InDeath") == false)
                {
                    target.SetData("HANDSHAKER", player);
                    target.SetData("REQUEST", "HANDSHAKE");
                    target.SetData("IS_REQUESTED", true);
                    Notify.Send(target, NotifyType.Warning, NotifyPosition.BottomCenter, $"Игрок ({player.Value}) хочет пожать Вам руку. Y/N - принять/отклонить", 3000);
                    Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы предложили игроку ({target.Value}) пожать руку.", 3000);
                }
                else Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно пожать руку игроку в данный момент", 3000);
            }
            else Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно пожать руку игроку в данный момент", 3000);
        }
        public static void hanshakeTarget(Player player)
        {
            if (!Main.Players.ContainsKey(player) || !player.HasData("HANDSHAKER") || !Main.Players.ContainsKey(player.GetData<Player>("HANDSHAKER"))) return;
            Player target = player.GetData<Player>("HANDSHAKER");
            if ((!player.HasData("CUFFED") && !player.HasSharedData("InDeath")) || player.HasData("CUFFED") && player.GetData<bool>("CUFFED") == false && player.HasSharedData("InDeath") && player.GetSharedData<bool>("InDeath") == false)
            {
                if ((!target.HasData("CUFFED") && !target.HasSharedData("InDeath")) || target.HasData("CUFFED") && target.GetData<bool>("CUFFED") == false && target.HasSharedData("InDeath") && target.GetSharedData<bool>("InDeath") == false)
                {
                    player.PlayAnimation("mp_ped_interaction", "handshake_guy_a", 39);
                    target.PlayAnimation("mp_ped_interaction", "handshake_guy_a", 39);

                    Trigger.ClientEvent(player, "newFriend", target);
                    Trigger.ClientEvent(target, "newFriend", player);

                    Main.OnAntiAnim(player);
                    Main.OnAntiAnim(target);

                    NAPI.Task.Run(() => { try { Main.OffAntiAnim(player); Main.OffAntiAnim(target); player.StopAnimation(); target.StopAnimation(); } catch { } }, 4500);
                }
            }
        }
        internal class SearchObject
        {
            public string Name { get; set; }
            public List<string> Weapons { get; set; }
            public List<string> Items { get; set; }
        }
        [RemoteEvent("stopAnim")]
        public static void animstop(Player player)
        {
            if (player.HasData("animData"))
            {
                player.PlayAnimation("rcmcollect_paperleadinout@", "kneeling_arrest_get_up", 33);
                player.ResetSharedData("animData");
                player.ResetData("animData");
                player.StopAnimation();
            }
        }
        [RemoteEvent("playAnim")]
        public static void playAnim(Player player, string ad, string an, int af)
        {
            player.SetSharedData("animData", true);
            player.SetData("animData", true);
            player.PlayAnimation(ad, an, af);
            Trigger.ClientEvent(player, "clinet::helpkeysonHUD", true, "BACKSPACE", "ОСТАНОВИТЬ АНИМАЦИЮ");
        }

        internal class Animation
        {
            public string Dictionary { get; }
            public string Name { get; }
            public int Flag { get; }
            public int StopDelay { get; }

            public Animation(string dict, string name, int flag, int stopDelay = -1)
            {
                Dictionary = dict;
                Name = name;
                Flag = flag;
                StopDelay = stopDelay;
            }
        }
    }
}
