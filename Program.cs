using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sendy.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerwijderWKUsers.Models;

namespace VerwijderWKUsers
{
    class Program
    {
        enum Arguments
        {
            Help,
            VerplaatsUsersNaarAndereLijst,
            UpdateLoginTokens
        }

        private static IConfiguration _configuration;

        public static async Task Main(string[] args)
        {
             var argument = HandleArgs(args);

            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            var sendySettings = _configuration.GetSection("SendyApiSettings").Get<SendyApiSettings>();
            var sendyClient = new SendyClient(new Uri(sendySettings.Endpoint), sendySettings.ApiKey);

            switch (argument)
            {
                case Arguments.VerplaatsUsersNaarAndereLijst:
                    await VerplaatsToernooiUsers(sendySettings, sendyClient);
                    break;
                case Arguments.UpdateLoginTokens:
                    await WerkActieveDeelnemersBij(sendySettings, sendyClient, args);
                    break;
                default:
                    Console.WriteLine("HELP");
                    Console.WriteLine("-t = UpdateLoginTokens");
                    Console.WriteLine("-v = Verplaats users naar andere mailinglijst");
                    break;
            }
            Console.WriteLine("Klaar!");
            Console.ReadLine();
        }

        private static Arguments HandleArgs(string[] args)
        {
            if (args.Length >= 1)
            {
                if (args[0] == "-t")
                    return Arguments.UpdateLoginTokens;
                if (args[0] == "-v")
                    return Arguments.VerplaatsUsersNaarAndereLijst;
            }
            return Arguments.Help;
        }

        private static async Task VerplaatsToernooiUsers(SendyApiSettings sendySettings, SendyClient sendyClient)
        {
            var users = await GetUsers();

            foreach (var user in users)
            {
                var customFields = new Dictionary<string, string>
                {
                    {"Language", user.Taal}
                };

                Console.WriteLine($"Verwijder user {user.Email}, {user.Id}");
                await sendyClient.DeleteSubscriber(user.Email, sendySettings.HuidigeDeelnemersLijstId);

                if (user.Nieuwsbrief)
                {
                    Console.WriteLine($"Voeg toe aan lijst {user.Email}, ({user.Id}");
                    await sendyClient.Subscribe(user.Email, user.Naam, sendySettings.VerplaatsNaarLijstId, customFields);
                }
            }
        }

        private static async Task WerkActieveDeelnemersBij(SendyApiSettings sendySettings, SendyClient sendyClient, string[] args)
        {
            var vanafUserId = 0;
            if (args.Length > 1)
                int.TryParse(args[1], out vanafUserId);

            await InsertLoginTokens();
            var users = await GetActieveUsers(vanafUserId);

            foreach (var user in users)
            {
                var customFields = new Dictionary<string, string>
                {
                    { "LoginToken", user.EmailLoginToken?.Token },
                    { "UserId", user.Id.ToString() },
                    { "Teamnaam", user.Teamnaam },
                    { "Nieuwsbrief", user.Nieuwsbrief.ToString() },
                    { "Reminder", user.Reminder.ToString() },
                    { "Language", user.Taal }
                };

                var result = await sendyClient.Subscribe(user.Email, user.Naam, sendySettings.HuidigeDeelnemersLijstId, customFields);
    
                //No success? Then the user is already subscribed. In case it's subscribed, the data will be updated.
                if (!result.IsSuccess)
                {
                    Console.WriteLine($"Email {user.Email} not subscribed (probably updated): {result.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine($"Email {user.Email} subscribed.");
                }
            }
        }

        private static async Task<List<User>> GetUsers()
        {
            using (var context = new VPDataContext(_configuration))
            {
                return await context.Users
                    .Where(u => 
                        new[] { 0, 84 }.Contains(u.WebsiteId) &&
                        !u.Actief)
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }
        }

        private static async Task<List<User>> GetActieveUsers(int vanafUserId)
        {
            using (var context = new VPDataContext(_configuration))
            {
                return await context.Users
                    .Include(u => u.EmailLoginToken)
                    .Where(u => u.Id > vanafUserId &&
                        new[] { 0, 84 }.Contains(u.WebsiteId) &&
                        u.Actief && u.Nieuwsbrief)
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }
        }

        private static async Task InsertLoginTokens()
        {
            using (var context = new VPDataContext(_configuration))
            {
                await context.Database.ExecuteSqlCommandAsync("InsertEmailLoginTokens");
            }
        }
    }
}
