using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EngineerUpdateProfile.Model;
using Azure.Messaging.ServiceBus;
using System.Text;
using Microsoft.Extensions.Configuration;
using EngineerUpdateProfile.Business;

namespace EngineerUpdateProfile
{
    public class EngineerUpdateProfile
    {
        private readonly ILogger<EngineerUpdateProfile> _logger;
        private readonly ServiceBusClient client;
        private readonly ServiceBusSender sender;
        private readonly IEngineerUpdateProfileBusiness _engineerUpdateProfileBusiness;

        public EngineerUpdateProfile(
            ILogger<EngineerUpdateProfile> logger,
            IConfiguration configuration,
            IEngineerUpdateProfileBusiness engineerUpdateProfileBusiness)
        {
            _logger = logger;
            // Create the clients that we'll use for sending and processing messages.
            client = new ServiceBusClient(configuration["AzureServiceBusConnectionString"]);
            sender = client.CreateSender(configuration["TopicNameToPublish"]);

            _engineerUpdateProfileBusiness = engineerUpdateProfileBusiness;
        }

        [FunctionName("EngineerUpdateProfile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "update-profile/{userId}")] HttpRequest req, int userId)
        {
            IActionResult result;

            try
            {
                DateTime updatedDateTime = DateTime.Now;
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                UserExpertiseLevel userExpertiseLevel = JsonConvert.DeserializeObject<UserExpertiseLevel>(requestBody);

                bool isUpdated = await _engineerUpdateProfileBusiness.UpdateUserProfileBusiness(userId, userExpertiseLevel, updatedDateTime);
                _logger.LogInformation("{date} : Message = execution Run of the EngineerUpdateProfile (HTTPTriggered AZURE Function) executed.", DateTime.UtcNow);

                if (isUpdated)
                {
                    UserProfile userProfile = new UserProfile()
                    {
                        UserId = userId,
                        TechnicalSkillExpertiseLevel = userExpertiseLevel.TechnicalSkillExpertiseLevel,
                        NonTechnicalSkillExpertiseLevel = userExpertiseLevel.NonTechnicalSkillExpertiseLevel,
                        UpdatedDate = updatedDateTime
                    };
                    PublishTopicToAzureServiceBus(userProfile);
                    result = new StatusCodeResult(StatusCodes.Status200OK);
                }
                else
                {
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unknown error occurred on the execution Run of the EngineerUpdateProfile (HTTPTriggered AZURE Function).");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }

        #region Publish Event to Service Bus Topic
        /// <summary>
        /// Publish Update Pofile event to updateprofiletopic (Azure Service Bus Code)
        /// </summary>
        /// <param name="userProfileForAdmin"></param>
        private async void PublishTopicToAzureServiceBus(UserProfile userProfileForAdmin)
        {
            // create a batch 
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            // try adding a message to the batch
            var encryptedMessageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(userProfileForAdmin));
            if (!messageBatch.TryAddMessage(new ServiceBusMessage(encryptedMessageBody)))
            {
                // if it is too large for the batch
                _logger.LogInformation("{date} : PublishTopicToAzureServiceBus of the UpdateProfile (HTTPTriggered AZURE Function) Failed. Message: The message is too large to fit in the batch. ", DateTime.UtcNow);
                throw new Exception($"The message {encryptedMessageBody} is too large to fit in the batch.");
            }

            try
            {
                // Use the producer client to send the batch of messages to the Service Bus topic
                await sender.SendMessagesAsync(messageBatch);
                _logger.LogInformation("{date} : PublishTopicToAzureServiceBus of the UpdateProfile (HTTPTriggered AZURE Function) completed. Message: Message has been published to the topic.", DateTime.UtcNow);
                Console.WriteLine($"Messages has been published to the topic.");
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
        #endregion
    }
}
