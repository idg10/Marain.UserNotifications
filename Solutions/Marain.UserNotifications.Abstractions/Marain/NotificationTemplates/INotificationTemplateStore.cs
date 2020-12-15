﻿// <copyright file="INotificationTemplateStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.NotificationTemplates
{
    using System.Threading.Tasks;
    using Marain.UserPreferences;

    /// <summary>
    /// Interface for a service that can store and retrieve templates.
    /// </summary>
    public interface INotificationTemplateStore
    {
        /// <summary>
        /// Stores the given template.
        /// </summary>
        /// <typeparam name="T">The Notification template object. </typeparam>
        /// <param name="notificationType">The notification type of the object that will be stored. </param>
        /// <param name="communicationType">The communication type of the object that will be stored. </param>
        /// <param name="template">The template to save.</param>
        /// <returns>The stored notification.</returns>
        Task<T> StoreAsync<T>(string notificationType, CommunicationType communicationType, T template);

        /// <summary>
        /// Retrieves template for the specified notification type.
        /// </summary>
        /// <typeparam name="T">The Notification template. </typeparam>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="communicationType">The communication type.</param>
        /// <returns>The template.</returns>
        Task<T> GetAsync<T>(string notificationType, CommunicationType communicationType);
    }
}