using System;
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
            public List<Item> StarterItems = new List<Item>();

            public override void Initialize()
            {
                ServerApi.Hooks.NetGetData.Register(this, OnGetData);
                ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
                PlayerHooks.PlayerPostLogin += PostLogin;

                Commands.ChatCommands.Add(new Command(new List<string>() { "starteritems.reset.*", "starteritems.reset.stats" }, ResetCharacter, "resetcharacter"));

                if (!Config.ReadConfig())
                {
                    Log.ConsoleError("Failed to read CustomStarterItems.json! Consider generating a new config file.");
                }

                if (Config.contents.EnableStarterItems)
                {
                    StarterItems = GetStarterItems(Config.contents.StarterItems);
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
                {
                    PlayerList.Add(player.Name);
                }
            }

            private void PostLogin(PlayerPostLoginEventArgs args)
            {
                if (args.Player == null)
                    return;
                TSPlayer player = args.Player;

                if (PlayerList.Contains(player.Name) && Config.contents.EnableStarterItems)
                {
                    ClearInventory(player);

                    foreach (Item item in StarterItems)
                    {
                        if (player.InventorySlotAvailable)
                        {
                            player.GiveItem(item.netID, item.name, item.width, item.height, 1);
                        }
                    }
                    PlayerList.Remove(player.Name);
                }
            }

            private void CleanInventory(int Who) //original method from ClearInvSSC to prevent exploits
            {
                if (!TShock.Config.ServerSideCharacter)
                {
                    Log.ConsoleError("[CustomStarterItems] This plugin will not work properly with ServerSidedCharacters disabled.");
                }

                if (TShock.Config.ServerSideCharacter && !TShock.Players[Who].IsLoggedIn)
                {
                    var player = TShock.Players[Who];
                    player.TPlayer.SpawnX = -1;
                    player.TPlayer.SpawnY = -1;
                    player.sX = -1;
                    player.sY = -1;

                    ClearInventory(player);
                }
            }

            private static List<Item> GetStarterItems(int[] id) //returns items from config
            {
                List<Item> list = new List<Item>();

                foreach (int item in id)
                {
                    list.Add(TShock.Utils.GetItemById(item));
                }
                return list;
            }

            private void ClearInventory(TSPlayer player) //The inventory clearing method from ClearInvSSC
            {
                for (int i = 0; i < NetItem.maxNetInventory; i++)
                {
                    if (i < NetItem.maxNetInventory - (NetItem.armorSlots + NetItem.dyeSlots))
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
                    if (TShock.Config.ServerSideCharacter)
                    {
                        if (player.Group.HasPermission("starteritems.reset.*") || player.Group.HasPermission("starteritems.reset.stats")) //resets player's stats
                        {
                            player.TPlayer.statLife = 100;
                            player.TPlayer.statLifeMax = 100;
                            player.TPlayer.statMana = 20;
                            player.TPlayer.statManaMax = 20;
                        }

                            ClearInventory(player);

                            if (Config.contents.EnableStarterItems)
                            {
                                foreach (Item item in StarterItems)
                                {
                                    if (player.InventorySlotAvailable)
                                    {
                                        player.GiveItem(item.netID, item.name, item.width, item.height, 1);
                                    }
                                }
                            }
                            else
                            {
                                if (player.InventorySlotAvailable)
                                {
                                    player.GiveItem(-13, "", 0, 0, 1); //copper pickaxe
                                    player.GiveItem(-16, "", 0, 0, 1); //copper axe
                                    player.GiveItem(-15, "", 0, 0, 1); //copper shortsword
                                }
                            }
                        player.SendSuccessMessage("Character reset to default!");
                    }
                }
            }
        }
            #endregion
}
