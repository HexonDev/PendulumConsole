using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;

namespace PendulumConsole
{
    class Program
    {
        private static SqlConnection connection;

        static void Main()
        {
            InitDbConnection();

            connection.Open();
            CreateTables();
            ProcessDataFile();

            connection.Close();

            Console.WriteLine("Nyomj meg egy gombot a kilépéshez");
            Console.ReadKey();
        }


        static void ProcessDataFile()
        {
            Console.WriteLine(@"Példa: C:\Users\Username\Desktop\pendulum.txt");
            Console.Write("Írja be a beolvasni kívánt fájl elérési útvonalát a fenti példa alapján: ");
            string path = Console.ReadLine();

            try
            {
                var sr = new StreamReader(path);

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
                        SqlCommand cmd = new SqlCommand();
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = connection;
                        string[] data = dataLine.Split(';');

                        if (currentAttribute == "[albums]")
                        {
                            string id = data[0];
                            string artist = data[1];
                            string title = data[2];
                            DateTime release = DateTime.Parse(data[3]);

                            cmd.CommandText = "INSERT INTO Albums (id, artist, title, release) VALUES(@id, @artist, @title, @release)";
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@artist", artist);
                            cmd.Parameters.AddWithValue("@title", title);
                            cmd.Parameters.AddWithValue("@release", release);
                        }
                        else if (currentAttribute == "[tracks]")
                        {
                            string title = data[0];
                            TimeSpan length = TimeSpan.Parse(data[1]);
                            string albumId = data[2];
                            string tinyURL = data[3];

                            cmd.CommandText = "INSERT INTO Albums (id, artist, title, release) VALUES(@id, @artist, @title, @release)";
                            cmd.Parameters.AddWithValue("@title", title);
                            cmd.Parameters.AddWithValue("@length", length);
                            cmd.Parameters.AddWithValue("@album", albumId);
                            cmd.Parameters.AddWithValue("@url", tinyURL);
                        }

                        cmd?.ExecuteNonQuery();
                    }
                }

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
                connection = new SqlConnection(@"Server = (localdb)\MSSQLLocalDB;
                            AttachDbFilename=|DataDirectory|\Resources\music.mdf;
                            Integrated Security=True;
                            Connect Timeout=5
                            ");
            }
            catch (Exception e)
            {
                CloseProgram(10, e);
                throw;
            }
        }

        static void CreateTables()
        {
            Console.WriteLine("Szeretné törölni az elöző adatokat? (Y/N): ");
            var key = Console.ReadKey();

            Console.WriteLine("");
            try
            {
                Console.WriteLine("Szükséges SQL műveletek végrehajtása...");

                string SqlString = GetSQLString(key.Key == ConsoleKey.Y);

                SqlCommand command = new SqlCommand(SqlString, connection);
                command.ExecuteNonQuery();

                Thread.Sleep(1000);
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
            connection.Close();

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

        static string GetSQLString(bool delete = false)
        {
            if (delete)
            {
                return @"DROP TABLE IF EXISTS Tracks;
                    DROP TABLE IF EXISTS Albums;

                    CREATE TABLE Albums (
	                    id VARCHAR(4) PRIMARY KEY,
	                    artist VARCHAR(255) NOT NULL,
	                    title VARCHAR(255) NOT NULL,
	                    release DATE
                    );

                    CREATE TABLE Tracks (
	                    id INT PRIMARY KEY IDENTITY,
	                    title VARCHAR(255) NOT NULL,
	                    length TIME,
	                    album VARCHAR(4) FOREIGN KEY REFERENCES Albums(Id),
	                    url VARCHAR(30) 
                    );";
            }
            else
            {
                return @"

                    IF OBJECT_ID('Albums', 'U') IS NULL CREATE TABLE Albums (
	                    id VARCHAR(4) PRIMARY KEY,
	                    artist VARCHAR(255) NOT NULL,
	                    title VARCHAR(255) NOT NULL,
	                    release DATE
                    );

                    IF OBJECT_ID('Tracks', 'U') IS NULL CREATE TABLE Tracks (
	                    id INT PRIMARY KEY IDENTITY,
	                    title VARCHAR(255) NOT NULL,
	                    length TIME,
	                    album VARCHAR(4) FOREIGN KEY REFERENCES Albums(Id),
	                    url VARCHAR(30) 
                    );";
            }
            
        }
    }
}
