using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace EchoBotWithCounter.Welcome
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    public class WelcomeUserBot : IBot
    {
        // Messages sent to the user.
        private const string WelcomeMessage = @"Bienvenido 'Aqui va el nombre de su tienda/marca', para nosotros es un gusto atenderlo";

        private const string InfoMessage = @"Para iniciar, por favor ingrese su nombre";

        // The bot state accessor object. Use this to access specific state properties.
        private readonly WelcomeUserStateAccessors _welcomeUserStateAccessors;

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeUserBot"/> class.
        /// </summary>
        /// <param name="statePropertyAccessor"> Bot state accessor object.</param>
        public WelcomeUserBot(WelcomeUserStateAccessors statePropertyAccessor)
        {
            _welcomeUserStateAccessors = statePropertyAccessor ?? throw new System.ArgumentNullException("state accessor can't be null");
        }

        /// <summary>
        /// Every conversation turn for our WelcomeUser Bot will call this method, including
        /// any type of activities such as ConversationUpdate or ContactRelationUpdate which
        /// are sent when a user joins a conversation.
        /// This bot doesn't use any dialogs; it's "single turn" processing, meaning a single
        /// request and response.
        /// This bot uses UserState to keep track of first message a user sends.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = new CancellationToken())
        {
            // use state accessor to extract the didBotWelcomeUser flag
            var didBotWelcomeUser = await _welcomeUserStateAccessors.WelcomeUserState.GetAsync(turnContext, () => new WelcomeUserState());

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Your bot should proactively send a welcome message to a personal chat the first time
                // (and only the first time) a user initiates a personal chat with your bot.
                if (didBotWelcomeUser.DidBotWelcomeUser == false)
                {
                    didBotWelcomeUser.DidBotWelcomeUser = true;
                    // Update user state flag to reflect bot handled first user interaction.
                    await _welcomeUserStateAccessors.WelcomeUserState.SetAsync(turnContext, didBotWelcomeUser);
                    await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);

                    // the channel should sends the user name in the 'From' object
                    var userName = turnContext.Activity.From.Name;
                    var name = turnContext.Activity.Text.ToLowerInvariant();

                    await turnContext.SendActivityAsync($"Bienvenido {name}, por favor de clic en la accion que desea realizar", cancellationToken: cancellationToken);
                    await SendIntroCardAsync(turnContext, cancellationToken);
                }
            }

            // Greet when users are added to the conversation.
            // Note that all channels do not send the conversation update activity.
            // If you find that this bot works in the emulator, but does not in
            // another channel the reason is most likely that the channel does not
            // send this activity.
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    // Iterate over all new members added to the conversation
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message
                        // the 'bot' is the recipient for events from the channel,
                        // turnContext.Activity.MembersAdded == turnContext.Activity.Recipient.Id indicates the
                        // bot was added to the conversation.
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                            await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            else
            {
                // Default behavior for all other type of activities.
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} activity detected");
            }

            // save any state changes made to your state objects.
            await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);
        }

        /// <summary>
        /// Sends an adaptive card greeting.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {

            var response = turnContext.Activity.CreateReply();

            var card = new HeroCard();
            card.Title = "Nombre de la tienda";
            card.Text = @"Seleccione la acción que desea realizar";
            // card.Images = new List<CardImage>() { new CardImage("https://goo.gl/6MbZCS") }; Opcional poner imagen
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Crear un pedido", null, "Crear un pedido", "Crear un pedido", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                new CardAction(ActionTypes.OpenUrl, "Ver estado de un pedido", null, "Ver estado de un pedido", "Ver estado de un pedido", "https://stackoverflow.com/questions/tagged/botframework"),
                new CardAction(ActionTypes.OpenUrl, "Cancelar un pedido", null, "Crear un pedido", "Cancelar un pedido", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
            };

            response.Attachments = new List<Attachment>() { card.ToAttachment() };
            await turnContext.SendActivityAsync(response, cancellationToken);
        }
    }
}
