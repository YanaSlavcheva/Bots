using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace ZaraCodeBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private const string HaveAskedForUsernameKey = "HaveAskedForUsername";
        private const string UsernameKey = "Username";

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            var haveAskedForUsername = false;
            var username = string.Empty;

            if (activity.Type == ActivityTypes.Message)
            {
                    // get stat of the client
                    var stateClient = activity.GetStateClient();

                    // get userData
                    var userData = stateClient.BotState.GetPrivateConversationData(
                        activity.ChannelId,
                        activity.Conversation.Id,
                        activity.From.Id);

                haveAskedForUsername = userData.GetProperty<bool>(HaveAskedForUsernameKey);
                username = userData.GetProperty<string>(UsernameKey);

                if (!haveAskedForUsername)
                {
                    // create reply
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"Hi, there, Zara Code Week fan!");
                    stringBuilder.Append($"\n");
                    stringBuilder.Append($"What is your name?!?");

                    var askForNameString = stringBuilder.ToString();

                    // save data that we asked for username
                    userData.SetProperty<bool>(HaveAskedForUsernameKey, true);

                    stateClient.BotState.SetPrivateConversationData(
                        activity.ChannelId,
                        activity.Conversation.Id,
                        activity.From.Id,
                        userData);

                    // send reply
                    var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    var reply = activity.CreateReply(askForNameString);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        userData.SetProperty<string>(UsernameKey, activity.Text);
                        stateClient.BotState.SetPrivateConversationData(
                            activity.ChannelId,
                            activity.Conversation.Id,
                            activity.From.Id,
                            userData);
                    }
                    else
                    {
                        if (activity.Text.ToLower() == "motivate me")
                        {
                            using (var httpClient = new HttpClient())
                            {
                                var url = "http://yanaslavcheva-001-site8.btempurl.com/api/quotes/random";
                                var result = httpClient.GetStringAsync(url).GetAwaiter().GetResult();

                                if (!string.IsNullOrWhiteSpace(result))
                                {
                                    var quoteObject = JsonConvert.DeserializeObject<QuoteDto>(result);
                                    var reply = activity.CreateReply($"\"{quoteObject.Content}\" by {quoteObject.Author}");
                                    var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                                    await connector.Conversations.ReplyToActivityAsync(reply);
                                }
                            }
                        }
                        else
                        {
                            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                            var reply = activity.CreateReply($"{username}, you just said: \"{activity.Text}\"");
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                }       
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
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
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }

    class QuoteDto {

        public string Author { get; set; }

        public string Content { get; set; }
    }
}
