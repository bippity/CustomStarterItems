﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using TShockAPI;
using TShockAPI.Hooks;
using Terraria;
using TerrariaApi.Server;

namespace CustomStarterItems
{
        [ApiVersion(1, 16)]
        public class CustomStarterItems : TerrariaPlugin
        {
            #region Plugin Info
            public override Version Version
            {
                get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
            }
            public override string Name
            {
                get { return "CustomStarterItems"; }
            }
            public override string Author
            {
                get { return "Bippity"; }
            }
            public override string Description
            {
                get { return "Customize starter items for new players."; }
            }
            public CustomStarterItems(Main game)
                : base(game)
            {
                Order = 4;
            }
            #endregion

            #region Initialize/Dispose
            public List<string> PlayerList = new List<string>();
            public List<string> StarterItems = new List<string>();
            public int startHealth, startMana;

            public override void Initialize()
            {
                ServerApi.Hooks.NetGetData.Register(this, OnGetData);
                ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
                PlayerHooks.PlayerPostLogin += PostLogin;

                Commands.ChatCommands.Add(new Command(new List<string>() {"starteritems.reset.stats", "starteritems.reset.inventory" }, ResetCharacter, "resetcharacter"));

                if (!Config.ReadConfig())
                {
                    Log.ConsoleError("Failed to read CustomStarterItems.json! Consider generating a new config file.");
                }

                if (Config.contents.EnableStarterItems)
                {
                    StarterItems = GetStarterItems(Config.contents.StarterItems);

                    if (Config.contents.startHealth > 500)
                        startHealth = 500;
                    else if (Config.contents.startHealth < 100)
                        startHealth = 100;
                    else
                        startHealth = Config.contents.startHealth;

                    if (Config.contents.startMana > 200)
                        startMana = 200;
                    else if (Config.contents.startMana < 20)
                        startMana = 20;
                    else
                        startMana = Config.contents.startMana;
                }
            }
     

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                    ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                    PlayerHooks.PlayerPostLogin -= PostLogin;
                }
                base.Dispose(disposing);
            }
            #endregion

            #region CustomStarterItems
            private void OnGetData(GetDataEventArgs args)
            {
                if (args.MsgID == PacketTypes.TileGetSection)
                {
                    if (Netplay.serverSock[args.Msg.whoAmI].state == 2)
                    {
                        CleanInventory(args.Msg.whoAmI);
                    }
                }
            }

            private void OnGreet(GreetPlayerEventArgs args)
            {
                if (TShock.Players[args.Who] == null)
                    return;

                TSPlayer player = TShock.Players[args.Who];
                var user = TShock.Users.GetUserByName(player.Name);

                if (user == null)
                    PlayerList.Add(player.Name);
            }

            private void PostLogin(PlayerPostLoginEventArgs args)
            {
                if (args.Player == null)
                    return;
                TSPlayer player = args.Player;

                if (player.UserAccountName != player.Name)  //checks if player logs in as same name
                    return;

                if (PlayerList.Contains(player.Name) && Config.contents.EnableStarterItems)
                {
                    ClearInventory(player);

                    player.TPlayer.statLife = startHealth;
                    player.TPlayer.statLifeMax = startHealth;
                    player.TPlayer.statMana = startMana;
                    player.TPlayer.statManaMax = startMana;

                    NetMessage.SendData(4, -1, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                    NetMessage.SendData(42, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                    NetMessage.SendData(16, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                    NetMessage.SendData(50, -1, -1, "", player.Index, 0f, 0f, 0f, 0);

                    NetMessage.SendData(4, player.Index, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                    NetMessage.SendData(42, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                    NetMessage.SendData(16, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                    NetMessage.SendData(50, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);

                    int slot = 0;
                    foreach (string item in StarterItems)
                    {
                        Item give;

                        if (item.Contains(":"))
                        {
                            give = TShock.Utils.GetItemById(int.Parse(item.Substring(0, item.IndexOf(":"))));
                            give.stack = int.Parse(item.Substring(item.IndexOf(":") + 1));
                        }
                        else
                        {
                            give = TShock.Utils.GetItemById(int.Parse(item));
                        }

                        if (player.InventorySlotAvailable)
                        {
                            player.TPlayer.inventory[slot] = give;
                            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, string.Empty, player.Index, slot);
                            slot++;
                        }
                    }
                    PlayerList.Remove(player.Name);
                }
            }

            private void CleanInventory(int Who) //original method from ClearInvSSC to prevent exploits
            {
                if (!Main.ServerSideCharacter)
                {
                    Log.ConsoleError("[CustomStarterItems] This plugin will not work properly with ServerSidedCharacters disabled.");
                }

                if (Main.ServerSideCharacter && !TShock.Players[Who].IsLoggedIn)
                {
                    var player = TShock.Players[Who];
                    player.TPlayer.SpawnX = -1;
                    player.TPlayer.SpawnY = -1;
                    player.sX = -1;
                    player.sY = -1;

                    ClearInventory(player);
                }
            }

            private static List<string> GetStarterItems(string[] id) //returns items from config
            {
                List<string> list = new List<string>();

                foreach (string item in id)
                {
                    list.Add(item);
                }
                return list;
            }

            private void ClearInventory(TSPlayer player) //The inventory clearing method from ClearInvSSC
            {
                for (int i = 0; i < NetItem.maxNetInventory; i++)
                {
                    if (i < NetItem.maxNetInventory - (NetItem.armorSlots + NetItem.dyeSlots)) //main inventory excluding the special slots
                    {
                        player.TPlayer.inventory[i].netDefaults(0);
                    }
                    else if (i < NetItem.maxNetInventory - NetItem.dyeSlots)
                    {
                        var index = i - (NetItem.maxNetInventory - (NetItem.armorSlots + NetItem.dyeSlots));
                        player.TPlayer.armor[index].netDefaults(0);
                    }
                    else
                    {
                        var index = i - (NetItem.maxNetInventory - NetItem.dyeSlots);
                        player.TPlayer.dye[index].netDefaults(0);
                    }
                }

                for (int k = 0; k < NetItem.maxNetInventory; k++)
                {
                    NetMessage.SendData(5, -1, -1, "", player.Index, (float)k, 0f, 0f, 0);
                }

                for (int k = 0; k < Player.maxBuffs; k++)
                {
                    player.TPlayer.buffType[k] = 0;
                }

                NetMessage.SendData(4, -1, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(42, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(16, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(50, -1, -1, "", player.Index, 0f, 0f, 0f, 0);

                for (int k = 0; k < NetItem.maxNetInventory; k++)
                {
                    NetMessage.SendData(5, player.Index, -1, "", player.Index, (float)k, 0f, 0f, 0);
                }

                for (int k = 0; k < Player.maxBuffs; k++)
                {
                    player.TPlayer.buffType[k] = 0;
                }

                NetMessage.SendData(4, player.Index, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(42, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(16, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(50, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
            }

            private void ResetCharacter(CommandArgs args)
            {
                TSPlayer player = args.Player;
                if (player != null)
                {
                    if (Main.ServerSideCharacter)
                    {
                        if (player.Group.HasPermission("starteritems.reset.*") || player.Group.HasPermission("starteritems.reset.stats")) //resets player's stats
                        {
                            if (Config.contents.EnableStarterItems)
                            {
                                player.TPlayer.statLife = startHealth;
                                player.TPlayer.statLifeMax = startHealth;
                                player.TPlayer.statMana = startMana;
                                player.TPlayer.statManaMax = startMana;
                            }
                            else
                            {
                                player.TPlayer.statLife = 100;
                                player.TPlayer.statLifeMax = 100;
                                player.TPlayer.statMana = 20;
                                player.TPlayer.statManaMax = 20;
                            }

                            NetMessage.SendData(4, -1, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(42, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(16, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(50, -1, -1, "", player.Index, 0f, 0f, 0f, 0);

                            NetMessage.SendData(4, player.Index, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(42, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(16, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(50, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                        }

                            ClearInventory(player);

                            if (Config.contents.EnableStarterItems)
                            {
                                int slot = 0;
                                    foreach (string item in StarterItems)
                                    {
                                        Item give;

                                        if (item.Contains(":"))
                                        {
                                            give = TShock.Utils.GetItemById(int.Parse(item.Substring(0, item.IndexOf(":"))));
                                            give.stack = int.Parse(item.Substring(item.IndexOf(":") + 1));
                                        }
                                        else
                                        {
                                            give = TShock.Utils.GetItemById(int.Parse(item));
                                        }

                                        if (player.InventorySlotAvailable)
                                        {
                                            player.TPlayer.inventory[slot] = give;
                                            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, string.Empty, player.Index, slot);
                                            slot++;
                                        }
                                    }
                            }
                            else
                            {
                                if (player.InventorySlotAvailable)
                                {
                                    player.TPlayer.inventory[0] = TShock.Utils.GetItemById(-13);
                                    player.TPlayer.inventory[1] = TShock.Utils.GetItemById(-16);
                                    player.TPlayer.inventory[2] = TShock.Utils.GetItemById(-15);
                                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, string.Empty, player.Index, 0);  //MsgType, ClientSomething, ClientSomething, string?, player, slot? what.
                                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, string.Empty, player.Index, 1);
                                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, string.Empty, player.Index, 2);
                                }
                            }
                        player.SendSuccessMessage("Character reset to default!");
                    }
                }
            }
        }
            #endregion
}
