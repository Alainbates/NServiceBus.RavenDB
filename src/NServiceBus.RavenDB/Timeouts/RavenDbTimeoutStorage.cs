﻿namespace NServiceBus.Features
{
    using System;
    using Persistence;
    using Raven.Client;
    using Raven.Client.Document;
    using RavenDB;
    using RavenDB.Internal;
    using TimeoutPersisters.RavenDB;

    /// <summary>
    /// RavenDB Timeout storage
    /// </summary>
    public class RavenDbTimeoutStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="RavenDbTimeoutStorage"/>.
        /// </summary>
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Try getting a document store object specific to this Feature that user may have wired in
            var store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbTimeoutSettingsExtenstions.SettingsKey);

            // Init up a new DocumentStore based on a connection string specific to this feature
            if (store == null)
            {
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB/Timeout");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                }                
            }

            // Trying pulling a shared DocumentStore set by the user or other Feature
            store = store ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Timeouts and no DocumentStore instance found");
            }

            new TimeoutsIndex().Execute(store);

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.DocumentStore, store)
                .ConfigureProperty(x => x.EndpointName, context.Settings.EndpointName())
                ;
        }
    }

    public static class RavenDbTimeoutSettingsExtenstions
    {
        public const string SettingsKey = "RavenDbDocumentStore/Timeouts";

        public static PersistenceConfiguration UseDocumentStoreForTimeouts(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}
