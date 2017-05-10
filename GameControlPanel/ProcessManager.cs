using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace GameControlPanel
{
    class ProcessManager
    {
        public ProcessManager()
        {
        }

        private static string ConnectionString = "Database=gameservers;Data Source=localhost;User Id=gameservers;Password=GSPassword1";
        private static MySqlConnection conn = new MySqlConnection(ConnectionString);

        private static Dictionary<int, GameFile> FetchGameServers()
        {
            Dictionary<int, GameFile> gs = new Dictionary<int, GameFile>();
            int count = 1;
            try
            {

                string sql = "SELECT Id, exepath, parameters, type, enabled  FROM data";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                GameFile gf = new GameFile();
                while (rdr.Read())
                {
                    gf.UniqueID = Convert.ToInt32(rdr["Id"]);
                    gf.ExecutablePath = rdr["exepath"].ToString();
                    gf.Parameters = rdr["parameters"].ToString();
                    gf.GameType = rdr["type"].ToString();
                    int enabled = Convert.ToInt32(rdr["enabled"]);
                    if (enabled == 1)
                    {
                        gf.AutoRestart = true;
                    }
                    else
                    {
                        gf.AutoRestart = false;
                    }
                    gf.PID = -1;
                    gs.Add(count, gf);
                    count++;
                }
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return gs;
        }

        public static Dictionary<int, GameFile> ActiveGameServers;

        public void Startup()
        {
            conn.Open();
            ActiveGameServers = FetchGameServers();
            foreach (KeyValuePair<int, GameFile> pair in ActiveGameServers)
            {
                if (pair.Value.AutoRestart)
                {
                    StartGame(pair.Value.UniqueID);
                }
            }
        }

        public static void UpdateActiveServers()
        {
            List<int> idlist = new List<int>();
            bool found = false;
            foreach (KeyValuePair<int, GameFile> pair in ActiveGameServers)
            {
                idlist.Add(pair.Value.UniqueID);
            }
            foreach (KeyValuePair<int, GameFile> pair in FetchGameServers())
            {
                found = false;
                foreach (int idl in idlist)
                {
                    if (idl == pair.Value.UniqueID)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ActiveGameServers.Add(ActiveGameServers.Count + 1, pair.Value);
                }
            }
        }

        public static void ChangeActEna(int enabled, int uniqueid)
        {
           /* 
            try
            {
                string sql = string.Format("UPDATE data SET enabled={0} WHERE Id={1}", enabled, uniqueid);
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.EndExecuteNonQuery(MysqlQueryDump);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }*/
        }

        public static void StartGame(int uniqueid)
        {
            UpdatePathParams(uniqueid);
            int index = -1;
            GameFile g = null;
            foreach (KeyValuePair<int, GameFile> pairs in ActiveGameServers)
            {
                if (pairs.Value.UniqueID == uniqueid)
                {
                    index = pairs.Key;
                    g = pairs.Value;
                }
            }
            if (index != -1 && g != null && g.PID == -1)
            {
                try
                {
                    Process p = new Process();
                    p.StartInfo.RedirectStandardOutput = false;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = g.ExecutablePath;
                    p.StartInfo.Arguments = g.Parameters;
                    p.EnableRaisingEvents = true;
                    p.Exited += GameExited;
                    p.Start();
                    ActiveGameServers[index].AutoRestart = true;
                    ActiveGameServers[index].PID = p.Id;
                    ChangeActEna(1, g.UniqueID);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        static void GameExited(object sender, EventArgs e)
        {
            Process p = (Process)sender;
            Console.WriteLine("game exited");
            foreach(KeyValuePair<int, GameFile> pair in ActiveGameServers)
            {
                if (pair.Value.PID != -1)
                {
                    if (pair.Value.PID == p.Id)
                    {
                        ActiveGameServers[pair.Key].PID = -1;
                        if (pair.Value.AutoRestart)
                        {
                            Console.WriteLine("restarting game");
                            StartGame(pair.Value.UniqueID);
                        }
                        else
                        {
                            ChangeActEna(0, pair.Value.UniqueID);
                        }
                    }
                }
            }
        }

        public static void StopGame(int uniqueid)
        {
            int index = -1;
            foreach (KeyValuePair<int, GameFile> pairs in ActiveGameServers)
            {
                if (pairs.Value.UniqueID == uniqueid)
                    index = pairs.Key;
            }
            if (index != -1)
            {
                if (ActiveGameServers[index].PID != -1)
                {
                    Process p = Process.GetProcessById(ActiveGameServers[index].PID);
                    ActiveGameServers[index].AutoRestart = false;
                    ActiveGameServers[index].PID = -1;
                    p.Kill();
                }
            }
            else 
            {
                Console.WriteLine("Server Not Found");
            }
        }

        public static void UpdatePathParams(int uniqueid)
        {
            int index = -1;
            GameFile g = null;
            foreach (KeyValuePair<int, GameFile> pairs in ActiveGameServers)
            {
                if (pairs.Value.UniqueID == uniqueid)
                {
                    index = pairs.Key;
                    g = pairs.Value;
                }
            }
            if (index != -1)
            {
                try
                {
                    string sql = string.Format("SELECT parameters FROM data WHERE Id={0}", g.UniqueID);
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        ActiveGameServers[index].Parameters = rdr["parameters"].ToString();
                    }
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }

    class GameFile
    {
        public GameFile()
        {
        }

        public int UniqueID { get; set; }
        public string ExecutablePath { get; set; }
        public string Parameters { get; set; }
        public string GameType { get; set; }
        public int PID { get; set; }
        public bool AutoRestart { get; set; }
    }
}
