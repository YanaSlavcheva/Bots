namespace ZaraCodeWeek.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Bot.Connector;
    using Newtonsoft.Json;

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private const string KeyAskedForUsernameString = "AskedForUsername";
        private const string KeyUsernameString = "Username";

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            //Global values
            var haveAskedForUsername = false;
            var username = string.Empty;

            if (activity.Type == ActivityTypes.Message)
            {
                // Get any saved values
                var stateClient = activity.GetStateClient();
                var userData = stateClient.BotState.GetPrivateConversationData(
                    activity.ChannelId,
                    activity.Conversation.Id,
                    activity.From.Id);
                haveAskedForUsername = userData.GetProperty<bool>(KeyAskedForUsernameString);
                username = userData.GetProperty<string>(KeyUsernameString) ?? string.Empty;

                // Create text for reply message
                var replyMessageStringBuilder = new StringBuilder();
                if (!haveAskedForUsername)
                {
                    replyMessageStringBuilder.Append($"Hello, I am **ZaraCodeWeek** Bot.");
                    replyMessageStringBuilder.Append($"\n");
                    replyMessageStringBuilder.Append($"You can say anything");
                    replyMessageStringBuilder.Append($"\n");
                    replyMessageStringBuilder.Append($"to me and I will repeat it back.");
                    replyMessageStringBuilder.Append($"\n\n");
                    replyMessageStringBuilder.Append($"What is your name?");

                    userData.SetProperty<bool>(KeyAskedForUsernameString, true);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        replyMessageStringBuilder.Append($"Hello {activity.Text}");

                        userData.SetProperty<string>(KeyUsernameString, activity.Text);
                    }
                    else
                    {
                        if (activity.Text.ToLower() == "gimme quote")
                        {
                            using (var httpClient = new HttpClient())
                            {
                                var url = "http://yanaslavcheva-001-site8.btempurl.com/api/quotes/random";
                                var result = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                                if (!string.IsNullOrWhiteSpace(result))
                                {
                                    var quote = JsonConvert.DeserializeObject<QuoteDto>(result);
                                    replyMessageStringBuilder.Append($"\"{quote.Content}\" by {quote.Author}");
                                }
                            }
                        }
                        else if (activity.Text.ToLower().StartsWith("gimme gif"))
                        {
                            var keyWord = activity
                                .Text
                                .Split(new string[] { "gimme gif" }, StringSplitOptions.RemoveEmptyEntries)
                                .FirstOrDefault()
                                ?.Trim();

                            var url = $"https://api.tenor.com/v1/search?q={keyWord}&key=LIVDSRZULELA";

                            using (var httpClient = new HttpClient())
                            {
                                var result = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                                if (!string.IsNullOrWhiteSpace(result))
                                {
                                    var gifs = JsonConvert.DeserializeObject<GifsDto>(result);

                                    var gif = gifs.Results.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                                    replyMessageStringBuilder.Append(gif?.Url ?? "No gifs found.");
                                }
                            }
                        }
                        else
                        {
                            replyMessageStringBuilder.Append($"{username}, you said: {activity.Text}");
                        }
                    }
                }

                // Save BotUserData
                stateClient.BotState.SetPrivateConversationData(
                    activity.ChannelId,
                    activity.Conversation.Id,
                    activity.From.Id,
                    userData);

                // Create reply message
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var replyMessage = activity.CreateReply(replyMessageStringBuilder.ToString());

                await connector.Conversations.ReplyToActivityAsync(replyMessage);
            }
            else
            {
                var replyMessage = HandleSystemMessage(activity);
                if (replyMessage != null)
                {
                    var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(replyMessage);
                }
            }

            // Return response
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Get BotUserData
                var stateClient = message.GetStateClient();
                var userData = stateClient.BotState.GetPrivateConversationData(
                    message.ChannelId,
                    message.Conversation.Id,
                    message.From.Id);

                // Set BotUserData
                userData.SetProperty<string>(KeyUsernameString, string.Empty);
                userData.SetProperty<bool>(KeyAskedForUsernameString, false);

                // Save BotUserData
                stateClient.BotState.SetPrivateConversationData(
                    message.ChannelId,
                    message.Conversation.Id,
                    message.From.Id,
                    userData);

                // Create a reply message
                var replyMessage = message.CreateReply("Personal data has been deleted.");
                return replyMessage;
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }

    class QuoteDto
    {
        public int Id { get; set; }

        public string Author { get; set; }

        public string Content { get; set; }
    }

    class GifsDto
    {
        public IEnumerable<GifDto> Results { get; set; }
    }

    class GifDto
    {
        public string Url { get; set; }
    }
}