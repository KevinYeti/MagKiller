using Dijkstra.NET.Contract;
using Dijkstra.NET.Model;
using Dijkstra.NET.ShortestPath;
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
        static Graph<string, string> _graph = null;
        static Dictionary<string, int> _nodes = null;
        static Dictionary<int, Position> _nodes2 = null;

        static void Main(string[] args)
        {
            string input = string.Empty;

            //ServerHelper.Initialize("http://106.75.33.221:7000/");
            ServerHelper.Initialize("http://dev.magcore.clawit.com/");

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
            _enemies.Clear();
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
            Task.Factory.StartNew(() =>
            {
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
                Thread.Sleep(30000);
                Console.WriteLine("Core protector started.");
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    PlayerHelper.GetPlayer(ref self);

                    foreach (var pos in self.Bases)
                    {
                        MapHelper.Attack(game.Id, self.Id, pos.X, pos.Y);

                        var siblings = pos.GetSiblings();
                        foreach (var sibling in siblings)
                        {
                            MapHelper.Attack(game.Id, self.Id, sibling.X, sibling.Y);
                        }
                    }

                    Thread.Sleep(3000);
                }
            });

            //foreach (var pos in self.Bases)
            //{
            //    Expend(5, pos);
            //}

            while (game.State != 1)
            {
                Thread.Sleep(100);
            }

            Rush(10);

            int c = 0;
            while (game.State == 1)
            {
                c++;
                if (c % 10 == 0)
                {
                    Rush(3);
                }
                
                Kill();

                Thread.Sleep(50);
            }


        }

        static void Expend(int second, Position center)
        {
            //Task.Factory.StartNew(()=> {
                for (int i = 0; i < second; i++)
                {
                    for (int j = 1; j <= 2; j++)
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


                    Thread.Sleep(2000);
                }
            //});
        }

        static void Kill()
        {
            foreach (var key in _enemies.Keys)
            {
                var enemy = _enemies[key];
                var routes = findP(key);
                if (routes != null)
                {
                    for (int i = 1; i < routes.Count; i++)
                    {
                        var route = routes[i];
                        var cell = game.Locate(route.X, route.Y);
                        if (cell.OwnerIndex != self.Index)
                        {
                            MapHelper.Attack(game.Id, self.Id, route.X, route.Y);
                            Thread.Sleep(10);
                        }
                        else
                            continue;
                        
                    }
                }
            }

        }

        static void initG()
        {
            _graph = new Graph<string, string>();
            _nodes = new Dictionary<string, int>();
            _nodes2 = new Dictionary<int, Position>();
            int i= 0;
            foreach (var row in game.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    _nodes.Add(cell.Key, i);
                    _nodes2.Add(i, cell.Position);
                    _graph.AddNode(cell.Key);
                    i++;
                }
            }
        }

        static object _locker = new object();

        static void updateG()
        {
            lock (_locker)
            {
                foreach (var row in game.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        int from = _nodes[cell.Key];
                        var siblings = cell.Position.GetSiblings();
                        foreach (var sibling in siblings)
                        {
                            var neighbor = game.Locate(sibling.X, sibling.Y);
                            if (neighbor == null)
                            {
                                continue;
                            }
                            int to = _nodes[neighbor.Key];
                            int cost = 0;
                            if (neighbor.Type == 0)
                                cost = 99999; //CellType == NULL
                            else
                            {
                                //neighbor.Type==1      //normal
                                if (neighbor.State == 0)
                                    cost = 1;   //无归属
                                else
                                {
                                    if (neighbor.OwnerIndex == self.Index)
                                    {
                                        cost = 0;
                                    }
                                    else
                                        cost = 15;
                                }

                            }
                            _graph.Connect((uint)from, (uint)to, cost, "");
                        }

                    }
                }
            }
        }

        static List<Position> findP(string key)
        {
            lock (_locker)
            {
                initG();
                updateG();

                List<Position> path = null;
                if (self.Bases != null && self.Bases.Count > 0
                    && self.Bases[0] != null)
                {
                    int from = _nodes[self.Bases[0].ToString()];
                    int to = _nodes[key];
                    Dijkstra<string, string> _dijkstra = new Dijkstra<string, string>(_graph);
                    IShortestPathResult result = _dijkstra.Process((uint)from, (uint)to); //result contains the shortest path
                    
                    if (result != null && result.IsFounded)
                    {
                        var routes = result.GetPath();
                        path = new List<Position>();
                        foreach (var route in routes)
                        {
                            Position pos = _nodes2[(int)route];
                            path.Add(pos);
                        }
                    }
                }

                return path;

            }
        }

        static void Rush(int second)
        {
            int ms = second * 1000;
            double run = 0;
            while (run < ms)
            {
                var start = DateTime.Now;

                foreach (var row in game.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        if (cell.Type != 0 && cell.State != 1
                            && cell.OwnerIndex == self.Index) //means this cell is self's
                        {
                            var siblings = cell.Position.GetSiblings();
                            foreach (var pos in siblings)
                            {
                                var target = game.Locate(pos.X, pos.Y);
                                if (target != null && target.Type != 0 && target.State != 1
                                    && target.OwnerIndex != self.Index)
                                {
                                    MapHelper.Attack(game.Id, self.Id, pos.X, pos.Y);
                                }
                            }
                        }
                    }
                }

                var ts = DateTime.Now - start;
                run += ts.TotalMilliseconds;
            }
            

        }
    }
}
