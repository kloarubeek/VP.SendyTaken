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
                    await WerkDeelnemersBij(sendySettings, sendyClient, args);
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
                await sendyClient.DeleteSubscriberAsync(user.Email, sendySettings.HuidigeDeelnemersLijstId);

                if (user.Nieuwsbrief)
                {
                    Console.WriteLine($"Voeg toe aan lijst {user.Email}, ({user.Id}");
                    await sendyClient.SubscribeAsync(user.Email, user.Naam, sendySettings.VerplaatsNaarLijstId, customFields);
                }
            }
        }

        private static async Task WerkDeelnemersBij(SendyApiSettings sendySettings, SendyClient sendyClient, string[] args)
        {
            var vanafUserId = 0;
            if (args.Length > 1)
                int.TryParse(args[1], out vanafUserId);

            await InsertLoginTokens();
            var users = await GetAllUsers(vanafUserId);

            foreach (var user in users)
            {
                var status = await sendyClient.SubscriptionStatusAsync(user.Email, sendySettings.HuidigeDeelnemersLijstId);

                // Al ingeschreven en nieuwsbrief ontvangen? Dan custom fields bijwerken
                // Bestaat nog niet in mailing list maar wel nieuwsbrief ontvangen? Dan toevoegen.
                if (
                    (string.Compare(status.Response, "Subscribed", StringComparison.OrdinalIgnoreCase) == 0 && user.Nieuwsbrief) ||
                    (!status.IsSuccess && string.Compare(status.ErrorMessage, "Email does not exist in list", StringComparison.OrdinalIgnoreCase) == 0 && user.Nieuwsbrief)
                )
                {
                    var customFields = new Dictionary<string, string>
                    {
                        { "LoginToken", user.EmailLoginToken?.Token },
                        { "UserId", user.Id.ToString() },
                        { "Teamnaam", user.Teamnaam },
                        { "Nieuwsbrief", user.Nieuwsbrief.ToString() },
                        { "Reminder", user.Reminder.ToString() },
                        { "Language", user.Taal },
                        { "IsActive", user.Actief.ToString() }
                    };
                    var result = await sendyClient.SubscribeAsync(user.Email, user.Naam, sendySettings.HuidigeDeelnemersLijstId, customFields);

                    Console.WriteLine($"{user.Id} {user.Email}: {result.IsSuccess} {result.ErrorMessage}{result.Response}");
                }
                else if(user.Nieuwsbrief == false)
                {
                    var result = await sendyClient.UnsubscribeAsync(user.Email, sendySettings.HuidigeDeelnemersLijstId);
                    Console.WriteLine($"{user.Id} {user.Email}: unsubscribed: {status.Response}");
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

        private static async Task<List<User>> GetAllUsers(int vanafUserId)
        {
            using (var context = new VPDataContext(_configuration))
            {
                return await context.Users
                    .Include(u => u.EmailLoginToken)
                    .Where(u => u.Id >= vanafUserId &&
                        new[] { 0, 84 }.Contains(u.WebsiteId))
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
