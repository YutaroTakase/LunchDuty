﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LunchDutyFunction
{
    public static class Function
	{
		private static readonly SlackAPI slack = new SlackAPI();
        private static ILogger logger;

		private static IConfigurationRoot Configuration { get; }

		static Function()
		{
			IConfigurationBuilder builder = new ConfigurationBuilder()
							.AddJsonFile("local.settings.json", true)
							.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		[FunctionName("Duty")]
		public static void Run([TimerTrigger("0 0 1 * * 1-5")]TimerInfo myTimer, ILogger log)
		{
            logger = log;

			var members = slack.GetMembers().ToList();

			log.LogInformation($"member count = {members.Count}");

			string duty = GetDuty(members);

			string message = GetMessage(duty);

			slack.Send(message);
		}

		private static string GetDuty(List<string> members)
		{
            var ticks = (int)DateTimeOffset.Now.Ticks;

            logger.LogInformation($"ticks = {ticks}");

            return members[new Random(ticks).Next(0, members.Count)];
		}

		private static string GetMessage(string name)
		{
			return $@"本日の給食当番は <@{name}> さんです！";
		}

		private class SlackAPI
		{
			private const string SLACK_API_BASE = "https://slack.com/api";

			private readonly HttpClient client = new HttpClient() { BaseAddress = new Uri(SLACK_API_BASE) };

			internal IEnumerable<string> GetMembers()
			{
				string query = CreateGetContent(
					token => Configuration["SlackToken"],
					channel => Configuration["SlackChannel"],
					include_locale => true
					);

				HttpResponseMessage response = client.GetAsync(new UriBuilder(Path.Combine(SLACK_API_BASE, "channels.info"))
				{
					Query = query
				}.ToString()).Result;

				string json = response.Content.ReadAsStringAsync().Result;

				dynamic jo = JsonConvert.DeserializeObject<dynamic>(json);

				foreach (dynamic member in jo.channel.members)
				{
					yield return member;
				}
			}

			internal void Send(string message)
			{
				const string username = "給食当番bot";
				const string iconEmoji = "";

				string json = JsonConvert.SerializeObject(new
				{
					username,
					text = message,
					icon_emoji = iconEmoji
				});

				using (var webClient = new WebClient())
				{
					webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json;charset=UTF-8");
					webClient.Encoding = Encoding.UTF8;
					webClient.UploadString(new Uri(Configuration["WebHook"]), json);
				}
			}

			private string CreateGetContent(params Expression<Func<object, object>>[] exprs)
			{
				NameValueCollection contents = HttpUtility.ParseQueryString(string.Empty);
				foreach (Expression<Func<object, object>> expr in exprs)
				{
					object obj = expr.Compile().Invoke(null);
					if (obj == null)
					{
						continue;
					}

					string param = obj.ToString();

					if (string.IsNullOrEmpty(param))
					{
						continue;
					}

					contents[expr.Parameters[0].Name] = HttpUtility.UrlEncode(param);
				}
				return contents.ToString();
			}
		}
	}
}
