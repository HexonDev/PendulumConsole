using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace PendulumConsole
{
    class Program
    {
        private static SqlConnection connection;
        static void Main()
        {
            InitDbConnection();
            CreateTables();
            ProcessDataFile();
            Console.ReadLine();
        }


        static void ProcessDataFile()
        {
            Console.WriteLine(@"Példa: C:\Users\Username\Desktop\pendulum.txt");
            Console.Write("Írja be a beolvasni kívánt fájl elérési útvonalát a fenti példa alapján: ");
            string path = Console.ReadLine();

            StreamReader sr;
            
            try
            {
                sr = new StreamReader(path);

                Console.Clear();
                Console.WriteLine("A feldolgozás folyamatban... kérem várjon");

                string currentAttribute = String.Empty;

                while (!sr.EndOfStream)
                {
                    string dataLine = sr.ReadLine();

                    if (dataLine == "[albums]" || dataLine == "[tracks]")
                    {
                        currentAttribute = dataLine;
                        Console.WriteLine("Jelenleg feldolgozás alatt: " + dataLine);
                    }

                    if (!LineIsAttribute(dataLine))
                    {
                        SqlCommand cmd = null;
                        if (currentAttribute == "[albums]")
                        {
                            string[] data = dataLine.Split(';');

                            string id = data[0];
                            string artist = data[1];
                            string title = data[2];
                            DateTime release = DateTime.Parse(data[3]);


                            string queryString = "INSERT INTO Albums (id, artist, title, release) VALUES(@id, @artist, @title, @release)";
                            cmd = new SqlCommand(queryString, connection);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@artist", artist);
                            cmd.Parameters.AddWithValue("@title", title);
                            cmd.Parameters.AddWithValue("@release", release);
                        }
                        else if (currentAttribute == "[tracks]")
                        {
                            string[] data = dataLine.Split(';');

                            string title = data[0];
                            TimeSpan length = TimeSpan.Parse(data[1]);
                            string albumId = data[2];
                            string tinyURL = data[3];

                            string queryString = "INSERT INTO Tracks (title, length, album, url) VALUES(@title, @length, @album, @url)";
                            cmd = new SqlCommand(queryString, connection);
                            cmd.Parameters.AddWithValue("@title", title);
                            cmd.Parameters.AddWithValue("@length", length);
                            cmd.Parameters.AddWithValue("@album", albumId);
                            cmd.Parameters.AddWithValue("@url", tinyURL);
                        }

                        cmd?.ExecuteNonQuery();
                        //

                    }
                }

                Thread.Sleep(2000);
                Console.WriteLine("A feldolgozás befejeződött!");
            }
            catch (Exception e)
            {
                CloseProgram(10, e);
                throw;
            }
        }
        static void InitDbConnection()
        {
            try
            {
                connection = new SqlConnection(@"Server = (localdb)\MSSQLLocalDB; Database = music; Trusted_Connection = True;");
            }
            catch (Exception e)
            {
                CloseProgram(10, e);
                throw;
            }
        }

        static void CreateTables()
        {
            try
            {
                Console.WriteLine("Táblák létrehozása...");
                
                string SqlString = File.ReadAllText("init.sql");

                connection.Open();

                SqlCommand command = new SqlCommand(SqlString, connection);
                command.ExecuteNonQuery();

                Console.WriteLine($"Táblák létrehozva!");

                Thread.Sleep(2000);
                Console.Clear();
            }
            catch (Exception e)
            {
                CloseProgram(10, e);
                throw;
            }
            
        }

        static void CloseProgram(int closeTime, Exception e)
        {
            Console.WriteLine();
            Console.WriteLine("[ H I B A ]");
            Console.WriteLine("A program hibát észlelt a futáskor:");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\t" + e.Message);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"A program {closeTime} másodperc múlva bezárul");
            Thread.Sleep(closeTime * 1000);
            Environment.Exit(0);
        }

        static bool LineIsAttribute(string line)
        {
            return line.Contains("[albums]") || line.Contains("[tracks]");
        }
    }
}
