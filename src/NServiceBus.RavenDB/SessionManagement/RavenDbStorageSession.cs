﻿namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using Features;
    using Internal;
    using NServiceBus.Persistence;
    using Raven.Client;
    using Raven.Client.Document;

    class RavenDbStorageSession : Feature
    {
        public RavenDbStorageSession()
        {
            DependsOnAtLeastOne(typeof(RavenDbSagaStorage));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getSessionFunc = context.Settings.GetOrDefault<Func<IDocumentSession>>(RavenDbSettingsExtenstions.SharedSessionSettingsKey);
            if (getSessionFunc != null)
            {
                context.Container.ConfigureComponent<ProvidedSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(x => x.GetSession, getSessionFunc);
                context.Pipeline.Register<ProvidedSessionBehavior.Registration>();
                return;
            }

            // Try getting a document store object specific to this Feature that user may have wired in
            var store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbSagaSettingsExtenstions.SettingsKey);

            // Init up a new DocumentStore based on a connection string specific to this feature
            if (store == null)
            {
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB/Saga");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                }
            }

            // Trying pulling a shared DocumentStore set by the user or other Feature
            store = store ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Sagas and no DocumentStore instance found");
            }

            context.Container.ConfigureComponent<RavenSessionProvider>(DependencyLifecycle.InstancePerCall);
            context.Container.RegisterSingleton<IDocumentStoreWrapper>(new DocumentStoreWrapper {DocumentStore = store}); // TODO needs a better wiring
            context.Pipeline.Register<OpenSessionBehavior.Registration>();
        }
    }
}
