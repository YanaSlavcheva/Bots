namespace ZaraCodeWeek.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Bot.Connector;

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
                        replyMessageStringBuilder.Append($"{username}, you said: {activity.Text}");
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
}