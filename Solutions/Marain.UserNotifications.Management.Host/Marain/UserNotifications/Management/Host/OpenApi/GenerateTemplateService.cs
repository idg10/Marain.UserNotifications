﻿// <copyright file="GenerateTemplateService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.UserNotifications.Management.Host.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Corvus.Tenancy;
    using DotLiquid;
    using Marain.NotificationTemplate.NotificationTemplate;
    using Marain.NotificationTemplate.NotificationTemplate.CommunicationTemplates;
    using Marain.Services.Tenancy;
    using Marain.UserPreferences;
    using Menes;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implements the generate template endpoint for the management API.
    /// </summary>
    public class GenerateTemplateService : IOpenApiService
    {
        /// <summary>
        /// The operation Id for the endpoint.
        /// </summary>
        public const string GenerateTemplateOperationId = "generateTemplate";

        private readonly IMarainServicesTenancy marainServicesTenancy;
        private readonly ITenantedNotificationTemplateStoreFactory tenantedTemplateStoreFactory;
        private readonly ITenantedUserPreferencesStoreFactory tenantedUserPreferencesStoreFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="GenerateTemplateService"/> class.
        /// </summary>
        /// <param name="marainServicesTenancy">Marain tenancy services.</param>
        /// <param name="tenantedTemplateStoreFactory">Template store factory.</param>
        /// <param name="tenantedUserPreferencesStoreFactory">User Preference store factory.</param>
        public GenerateTemplateService(
            IMarainServicesTenancy marainServicesTenancy,
            ITenantedNotificationTemplateStoreFactory tenantedTemplateStoreFactory,
            ITenantedUserPreferencesStoreFactory tenantedUserPreferencesStoreFactory)
        {
            this.marainServicesTenancy = marainServicesTenancy;
            this.tenantedTemplateStoreFactory = tenantedTemplateStoreFactory;
            this.tenantedUserPreferencesStoreFactory = tenantedUserPreferencesStoreFactory;
        }

        /// <summary>
        /// Generates a NotificationTypeTemplate which contains populated notification templates for different communication types for the user.
        /// </summary>
        /// <param name="context">The current OpenApi context.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Confirms that the create / update operation request is successful.</returns>
        [OperationId(GenerateTemplateOperationId)]
        public async Task<OpenApiResult> GenerateTemplateAsync(
            IOpenApiContext context,
            CreateNotificationsRequest body)
        {
            // We can guarantee tenant Id is available because it's part of the Uri.
            ITenant tenant = await this.marainServicesTenancy.GetRequestingTenantAsync(context.CurrentTenantId!).ConfigureAwait(false);

            // Get the UserPreferencesStore for the tenant
            IUserPreferencesStore userPreferencesStore = await this.tenantedUserPreferencesStoreFactory.GetUserPreferencesStoreForTenantAsync(tenant).ConfigureAwait(false);

            // Get the user preference for the userId
            UserPreference? userPreference = await userPreferencesStore.GetAsync(body.UserIds[0]).ConfigureAwait(false);

            // Check if the user has set the communication channels for the incoming notification type
            if (userPreference == null)
            {
                throw new Exception($"There is no user preference set up for this user {body.UserIds[0]} for tenant {tenant.Id}");
            }

            if (userPreference.CommunicationChannelsPerNotificationConfiguration == null)
            {
                throw new Exception($"There are no communication channel set up for the user {body.UserIds[0]} for tenant {tenant.Id}");
            }

            if (!userPreference.CommunicationChannelsPerNotificationConfiguration.ContainsKey(body.NotificationType))
            {
                throw new Exception($"There is no communication channel set up for the user {body.UserIds[0]} for notification type {body.NotificationType} for tenant {tenant.Id}");
            }

            List<CommunicationType>? registeredCommunicationChannels = userPreference.CommunicationChannelsPerNotificationConfiguration[body.NotificationType];

            if (registeredCommunicationChannels == null || registeredCommunicationChannels.Count == 0)
            {
                throw new Exception($"There are no communication channel set up for the user {body.UserIds[0]} for notification type {body.NotificationType} for tenant {tenant.Id}");
            }

            // Gets the AzureBlobTemplateStore
            INotificationTemplateStore templateStore = await this.tenantedTemplateStoreFactory.GetTemplateStoreForTenantAsync(tenant).ConfigureAwait(false);

            // Get the notification template for the notification type
            NotificationTemplate? rawNotificationTypeTemplate = await templateStore.GetAsync(body.NotificationType).ConfigureAwait(false);

            if (rawNotificationTypeTemplate == null)
            {
                throw new Exception($"There is no notification template set up for the user {body.UserIds[0]} for notification type {body.NotificationType} for tenant {tenant.Id}");
            }

            var communicationTypeDeliveryStatus = new Dictionary<CommunicationType, bool>();
            EmailTemplate? emailTemplate = null;
            SmsTemplate? smsTemplate = null;

            foreach (CommunicationType channel in registeredCommunicationChannels)
            {
                switch (channel)
                {
                    case CommunicationType.Email:

                        if (rawNotificationTypeTemplate.EmailTemplate != null && body.Properties != null)
                        {
                            Dictionary<string, object> existingProperties = PropertyBagHelpers.GetDictionaryFromPropertyBag(body.Properties);

                            // Email
                            var emailBodyTemplate = Template.Parse(rawNotificationTypeTemplate.EmailTemplate.Body);
                            string templatedBody = await emailBodyTemplate.RenderAsync(Hash.FromDictionary(existingProperties));

                            // Subject
                            var subjectTemplate = Template.Parse(rawNotificationTypeTemplate.EmailTemplate.Subject);
                            string templatedSubject = await subjectTemplate.RenderAsync(Hash.FromDictionary(existingProperties));

                            emailTemplate = new EmailTemplate(templatedBody, templatedSubject, false);
                            communicationTypeDeliveryStatus.Add(channel, true);
                            break;
                        }

                        communicationTypeDeliveryStatus.Add(channel, false);
                        break;
                    case CommunicationType.Sms:
                        if (rawNotificationTypeTemplate.SmsTemplate != null && body.Properties != null)
                        {
                            // SMS
                            var smsBodyTemplate = Template.Parse(rawNotificationTypeTemplate.SmsTemplate.Body);

                            Dictionary<string, object> existingProperties = PropertyBagHelpers.GetDictionaryFromPropertyBag(body.Properties);
                            string smsTemplatedBody = await smsBodyTemplate.RenderAsync(Hash.FromDictionary(existingProperties));

                            smsTemplate = new SmsTemplate(smsTemplatedBody);
                            communicationTypeDeliveryStatus.Add(channel, true);
                            break;
                        }

                        communicationTypeDeliveryStatus.Add(channel, false);
                        break;
                    case CommunicationType.WebPush:
                        break;
                    case CommunicationType.MMS:
                        break;
                }
            }

            var responseTemplate = new NotificationTemplate(
                body.NotificationType,
                smsTemplate: smsTemplate,
                emailTemplate: emailTemplate);

            // and replace with the tags inside the template with the ones received from the property bag in the CreateNotificationsRequest
            return this.OkResult(responseTemplate);
        }
    }
}