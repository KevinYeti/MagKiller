using MagCore.Sdk.Helper;
using MagCore.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MagKiller.App
{
    class Program
    {
        static Player self = null;
        static Map map = null;
        static Game game = null;

        static Dictionary<string, Position> _enemies = new Dictionary<string, Position>();

        static void Main(string[] args)
        {
            string input = string.Empty;

            ServerHelper.Initialize("http://106.75.33.221:7000/");
            //ServerHelper.Initialize("http://localhost:6000/");

            Player:
            Console.WriteLine("Enter nickname:");
            input = Console.ReadLine();
            string name = input.Trim();

            Console.WriteLine("Enter color(0~9):");
            input = Console.ReadLine();
            int color = Int32.Parse(input);

            self = PlayerHelper.CreatePlayer(name, color);
            if (self == null)
            {
                Console.WriteLine("Player has already existed with same name. Try to get a new name.");
                goto Player;
            }

            string gameId = string.Empty;
            string mapName = string.Empty;
            Console.WriteLine("1: Create a new game");
            Console.WriteLine("2: Join a game");
            input = Console.ReadLine();
            if (input == "1")
            {
                map = MapHelper.GetMap("RectSmall");
                game = new Game(map.Rows.Count, map.Rows[0].Count);
                gameId = GameHelper.CreateGame("RectSmall");

            }
            else
            {
                Console.WriteLine("Game list:");
                List:
                var list = GameHelper.GameList();
                if (list == null || list.Length == 0)
                {
                    Thread.Sleep(1000);
                    goto List;
                }
                else
                {
                    for (int i = 0; i < list.Length; i++)
                    {
                        if (list[i].state == 0)
                        {
                            Console.WriteLine("{0} : {1} 地图:{2}", i, list[i].id.ToString(), list[i].map.ToString());
                        }

                    }
                }
                Console.WriteLine("Select a game to join:");
                input = Console.ReadLine();
                if (Int32.TryParse(input.Trim(), out int sel)
                    && sel >= 0 && sel < list.Length)
                {
                    gameId = list[sel].id.ToString();
                    mapName = list[sel].map.ToString();

                    map = MapHelper.GetMap(mapName);
                    game = new Game(map.Rows.Count, map.Rows[0].Count);

                }
                else
                {
                    Console.WriteLine("Select error.");
                    goto List;
                }
            }


            game.Id = gameId;

            if (!GameHelper.JoinGame(gameId, self.Id))
                Console.WriteLine("Join game fail.");
            else
                Console.WriteLine("Join game Ok.");

            PlayerHelper.GetPlayer(ref self);
            Console.WriteLine("Self info updated.");

            //找到地图上所有的基地, 为杀手准备
            GameHelper.GetGame(game.Id, ref game);
            foreach (var row in game.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    if (cell.Type == 2 && cell.OwnerIndex != self.Index)
                    {
                        _enemies.Add(cell.Position.ToString(), cell.Position);
                    }
                }
            }
            
            //新开一个线程用于更新最新战斗数据
            Task.Factory.StartNew(() => {
                Thread.CurrentThread.IsBackground = true;

                while (true)
                {
                    GameHelper.GetGame(game.Id, ref game);

                    if (game.State > 1)
                    {
                        Console.WriteLine("Game over");
                        break;
                    }
                    Thread.Sleep(500);
                }
            });
            
            Attack();
        }

        static void Attack()
        {
            while (game.State == 0)
            {
                Thread.Sleep(100);
            }
            //create a thread to protect self 
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(20000);
                Console.WriteLine("Core protector started.");
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    PlayerHelper.GetPlayer(ref self);

                    foreach (var pos in self.Bases)
                    {
                        MapHelper.Attack(game.Id, self.Id, pos.X, pos.Y);

                        //var siblings = pos.GetSiblings();
                        //foreach (var sibling in siblings)
                        //{
                        //    MapHelper.Attack(game.Id, self.Id, sibling.X, sibling.Y);
                        //}
                    }

                    Thread.Sleep(1000);
                }
            });

            while (game.State == 1)
            {
                foreach (var pos in self.Bases)
                {
                    Expend(20, pos);
                }
                Thread.Sleep(5);

                Thread.Sleep(0);
            }


        }

        static void Expend(int second, Position center)
        {
            Task.Factory.StartNew(()=> {
                for (int i = 0; i < second; i++)
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        int x = center.X;
                        int y = center.Y;

                        x = center.X - j;
                        y = center.Y - j;
                        var target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X - j ;
                        y = center.Y;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X - j;
                        y = center.Y + j;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X;
                        y = center.Y - j;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X;
                        y = center.Y + j;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X + j;
                        y = center.Y - j;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X + j;
                        y = center.Y;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                        x = center.X + j;
                        y = center.Y + j;
                        target = game.Locate(x, y);
                        if (target != null && target.Type != 0 && target.State != 1
                                        && target.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, x, y);
                        }

                    }


                    Thread.Sleep(1000);
                }
            });
        }

        static void Kill()
        {
            Task.Factory.StartNew(() =>
            {
                while (game.State == 1)
                {


                }

                    
            });
        }

    }
}
