using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Controls;
using MySql.Data.MySqlClient;

namespace banggood.com_scraper.Models
{
    public static class Utility
    {
        public static string ConnectionString = "Data Source=system.db;Version=3;";
        public static string SimpleDateFormat = "dd/MM/yyyy HH:mm:ss";
        public static string mySqlConnection = "server=localhost;port=3306;database=saber;user=root;password=0988;";//spike  Jmz7k5BFeWMx! 104.238.214.40 localhost
        public static Dictionary<string, string> Config = new Dictionary<string, string>();
        //Used to load UI components last state from config dictionary
        public static void InitCntrl(Control parent)
        {
            try
            {
                foreach (Control x in parent.Controls)
                {
                    try
                    {
                        if (x.Name.EndsWith("I"))
                        {
                            switch (x)
                            {
                                case MetroCheckBox _:
                                    ((MetroCheckBox)x).Checked = bool.Parse(Config[x.Name]);
                                    break;
                                case CheckBox _:
                                    ((CheckBox)x).Checked = bool.Parse(Config[x.Name]);
                                    break;
                                case RadioButton _:
                                    ((RadioButton)x).Checked = bool.Parse(Config[x.Name]);
                                    break;
                                case TextBox _:
                                case RichTextBox _:
                                case MetroTextBox _:
                                    x.Text = Config[x.Name];
                                    break;
                                case NumericUpDown _:
                                    ((NumericUpDown)x).Value = int.Parse(Config[x.Name]);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    InitCntrl(x);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        //Used to save UI components last state to config dictionary
        public static void SaveCntrl(Control parent)
        {
            try
            {
                foreach (Control x in parent.Controls)
                {
                    #region Add key value to disctionarry

                    if (x.Name.EndsWith("I"))
                    {
                        switch (x)
                        {
                            case MetroCheckBox _:
                                Config.Add(x.Name, ((MetroCheckBox)x).Checked + "");
                                break;
                            case CheckBox _:
                                Config.Add(x.Name, ((CheckBox)x).Checked + "");
                                break;
                            case RadioButton _:
                                Config.Add(x.Name, ((RadioButton)x).Checked + "");
                                break;
                            case TextBox _:
                            case RichTextBox _:
                            case MetroTextBox _:
                                Config.Add(x.Name, x.Text);
                                break;
                            case NumericUpDown _:
                                Config.Add(x.Name, ((NumericUpDown)x).Value + "");
                                break;
                            default:
                                Console.WriteLine(@"could not find a type for " + x.Name);
                                break;
                        }
                    }
                    #endregion
                    SaveCntrl(x);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void CreateDb()
        {
            if (File.Exists("system.db"))
            {
                return;
            }
            try
            {
                SQLiteConnection.CreateFile("system.db");
                using (SQLiteConnection con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    string sql = "CREATE TABLE [config] ([key] varchar(20) PRIMARY KEY NOT NULL,[value] varchar(100) NOT NULL);";
                    using (var cd = new SQLiteCommand(sql, con))
                    {
                        cd.ExecuteNonQuery();
                    }
                    con.Close();
                }
                Console.WriteLine(@"db created");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static void SaveConfig()
        {
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(ConnectionString))
                {
                    con.Open();
                    foreach (KeyValuePair<string, string> kvp in Config)
                    {
                        string sql = String.Format("insert OR IGNORE into config (key,value) values ('{0}','{1}'); Update config set value = '{1}' where key='{0}'", kvp.Key, kvp.Value);
                        using (var cd = new SQLiteCommand(sql, con))
                        {
                            cd.ExecuteNonQuery();
                        }
                    }
                }
                Console.WriteLine(@"config saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static void LoadConfig()
        {
            Config = new Dictionary<string, string>();
            try
            {
                using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
                {
                    c.Open();
                    using (SQLiteCommand fmd = c.CreateCommand())
                    {
                        fmd.CommandText = @"SELECT * from config";
                        fmd.CommandType = CommandType.Text;
                        using (SQLiteDataReader r = fmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                Config.Add(r["key"].ToString(), r["value"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public static async Task<string> ExecuteBatch(string sql)
        {
            //Console.WriteLine(sql);
            try
            {
                using (var con = new MySqlConnection(mySqlConnection))
                {
                    await con.OpenAsync().ConfigureAwait(false);
                    using (var cd = new MySqlCommand(sql, con))
                        await cd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                return null;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
        public static async Task<(HashSet<string> urls, string error)> GetUrls()
        {
            var urls = new HashSet<string>();
            try
            {
                using (var c = new MySqlConnection(mySqlConnection))
                {
                    await c.OpenAsync().ConfigureAwait(false);
                    using (var fmd = new MySqlCommand("SELECT ProductUrl from products", c))
                    using (var r = (MySqlDataReader)await fmd.ExecuteReaderAsync().ConfigureAwait(false))
                        while (await r.ReadAsync().ConfigureAwait(false))
                            urls.Add(r.GetString("ProductUrl"));

                }
                return (urls, null);
            }
            catch (Exception ex)
            {
                return (null, ex.ToString());
            }
        }
    }
}
