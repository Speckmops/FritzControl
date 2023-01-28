/*
 * FritzControl .NET6
 * An experimental code to enable or disable static routes in a Fritzbox
 * 
 * speckmops.de
 * https://github.com/Speckmops/FritzControl
 */

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FritzControl
{
    internal class Program
    {
        private static WebClient Client = new();
        private static string? Username { get; set; }
        private static string? Password { get; set; }
        private static string? IP { get; set; }
        private static string? SessionId { get; set; }
        private static bool LoggedIn => SessionId != null && SessionId.Replace("0", "").Length > 0;
        private static StaticRouteTable? Routes { get; set; }

        #region Methods/Actions
        private static void Login()
        {
            if(Username == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Username: ");
                Console.ForegroundColor = ConsoleColor.White;
                Username = Console.ReadLine();
            }
            if (Password == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Password: ");
                Console.ForegroundColor = ConsoleColor.White;
                Password = Console.ReadLine();
            }
            if (IP == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"IP: ");
                Console.ForegroundColor = ConsoleColor.White;
                IP = Console.ReadLine();
            }

            Console.WriteLine("Logging in..");

            //Get challenge
            string loginResponse = Client.DownloadString($"http://{IP}/login_sid.lua");

            //Get challenge from XML-Response
            Match challengeMatch = new Regex("<Challenge>([A-Za-z0-9]+)</Challenge>", RegexOptions.IgnoreCase).Match(loginResponse);

            if (challengeMatch.Success && challengeMatch.Groups.Count == 2)
            {
                string challenge = challengeMatch.Groups[1].Value;
                Console.WriteLine($"Got Challenge: {challenge}");
                string response = GenerateResponse(challenge);
                Console.WriteLine($"Generated response: {response}");
                Console.WriteLine("Try login..");
                string result = Client.DownloadString($"http://{IP}/login_sid.lua?username={Username}&response={response}");
                Match sidMatch = new Regex("<SID>([A-Za-z0-9]+)</SID>", RegexOptions.IgnoreCase).Match(result);
                if (sidMatch.Success && sidMatch.Groups.Count == 2 && sidMatch.Groups[1].Value.Replace("0", "").Length > 0)
                {
                    SessionId = sidMatch.Groups[1].Value;
                    Console.WriteLine($"SessionId: {SessionId}");
                    ConsoleSuccess("Login successful!");
                }
                else
                {
                    Console.WriteLine("Login failed!");
                }
            }
            else
            {
                Console.WriteLine("Error: Can not load login challenge - login failed!");
            }
        }

        private static void List()
        {
            if (!LoggedIn)
            {
                Login();
            }
            if (!LoggedIn)
            {
                return;
            }
            NameValueCollection nameValueCollection = new NameValueCollection
            {
                { "sid", SessionId },
                { "xhr", "1" },
                { "lang", "de" },
                { "page", "static_route_table" },
                { "xhrId", "all" }
            };

            Routes = JsonSerializer.Deserialize<StaticRouteTable>(Encoding.UTF8.GetString(Client.UploadValues($"http://{IP}/data.lua", nameValueCollection)));

            if (Routes != null)
            {
                foreach (Route route in Routes.data.staticRoutes.route)
                {
                    Console.WriteLine(route);
                }
            }

        }

        private static void Toggle(bool activate)
        {
            if (!LoggedIn || Routes == null)
            {
                List();
            }
            if (!LoggedIn || Routes == null)
            {
                ConsoleError("Cant load route information");
                return;
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{(activate ? "Activate" : "Deactivate")} id: ");
            Console.ForegroundColor = ConsoleColor.White;
            string? cin = Console.ReadLine();

            if (cin != null)
            {
                int.TryParse(cin, out int routeId);
                if (routeId < 0 || routeId >= Routes.data.staticRoutes.route.Count)
                {
                    ConsoleError("Route does not exist");
                    return;
                }

                NameValueCollection nameValueCollection = new NameValueCollection
                {
                    { "sid", SessionId },
                    { "sidRenew", "false" },
                    { "apply", "" },
                    { "page", "static_route_table" },
                };

                for (int i = 0; i < Routes.data.staticRoutes.route.Count; i++)
                {
                    nameValueCollection.Add($"activatedroute{i}", i == routeId ? (activate ? "1" : "0") : Routes.data.staticRoutes.route[i].activated);
                }

                Routes = JsonSerializer.Deserialize<StaticRouteTable>(Encoding.UTF8.GetString(Client.UploadValues($"http://{IP}/data.lua", nameValueCollection)));

                if (Routes != null)
                {
                    foreach (Route route in Routes.data.staticRoutes.route)
                    {
                        Console.WriteLine(route);
                    }
                }
            }
        }
        #endregion

        #region Main
        static void Main(string[] args)
        {
            bool quit = false;

            Console.BackgroundColor = ConsoleColor.Black;

            do
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Action: ");
                Console.ForegroundColor = ConsoleColor.White;

                switch (Console.ReadLine()?.ToLower())
                {
                    case "login":
                        Login();
                        break;
                    case "list":
                        List();
                        break;
                    case "activate":
                        Toggle(true);
                        break;
                    case "deactivate":
                        Toggle(false);
                        break;
                    case "quit":
                        quit = true;
                        break;
                    case "exit":
                        quit = true;
                        break;
                    case "help":
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("");
                        Console.WriteLine("Available commands:");
                        Console.WriteLine("");
                        Console.WriteLine("login\t\t\tLogin");
                        Console.WriteLine("list\t\t\tList all routes");
                        Console.WriteLine("activate\t\tActivate a specific route");
                        Console.WriteLine("deactivate\t\tDeactivate a specific route");
                        Console.WriteLine("exit/quit");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    default:
                        ConsoleError("Unknown command, try 'help' for command information");
                        break;
                }
                Console.WriteLine();
            } while (!quit);
        }
        #endregion

        #region Help
        public static string MD5_UTF16_LE(string input)
        {

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.Unicode.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hash);
            }
        }
        private static void ConsoleError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void ConsoleSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static string GenerateResponse(string challenge) => challenge + "-" + MD5_UTF16_LE(challenge + "-" + Password).ToLower();
        #endregion
    }
}