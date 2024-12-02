using System.Threading.Tasks;
using DnsClient.Internal;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Play.Identity.Contracts;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;
using Microsoft.Extensions.Logging;
using ILogger = DnsClient.Internal.ILogger;

namespace Play.Identity.Service.Consumers
{
    public class DebitGilConsumer : IConsumer<DebitGil>
    {
        private readonly UserManager<ApplicationUser> userManager;

        private readonly ILogger<DebitGilConsumer> logger;

        public DebitGilConsumer(UserManager<ApplicationUser> userManager, ILogger<DebitGilConsumer> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task Consume(ConsumeContext<DebitGil> context)
        {
            var message = context.Message;
            
            logger.LogInformation(
                "Received debit gil request of {Quantity} from user {UserId} with CorrelationId {CorrelationId}", 
                message.Gil,
                message.UserId,
                message.CorrelationId);

            var user = await userManager.FindByIdAsync(message.UserId.ToString());

            if (user == null)
            {
                throw new UnknownUserException(message.UserId);
            }

            // Avoid duplicates
            if (user.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new GilDebited(message.CorrelationId));
                return;
            }

            user.Gil -= message.Gil;

            if (user.Gil < 0)
            {
                logger.LogError(
                    "Unable to debit gil of {Quantity} from user {UserId} with CorrelationId {CorrelationId}", 
                    message.Gil,
                    message.UserId,
                    message.CorrelationId);
                
                throw new InsufficientFundsException(message.UserId, message.Gil);
            }

            // Add the MessageId to check for duplicate messages
            user.MessageIds.Add(context.MessageId.Value);

            await userManager.UpdateAsync(user);

            var gilDebitedTask = context.Publish(new GilDebited(message.CorrelationId));
            var userUpdatedTask = context.Publish(new UserUpdated(user.Id, user.Email, user.Gil));

            await Task.WhenAll(userUpdatedTask, gilDebitedTask);
        }
    }
}