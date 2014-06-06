using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.RavenDB;
using NServiceBus.RavenDB.Persistence;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

[TestFixture]
public class When_configuring_raven_persistence
{

    [Test]
    public void Ensure_defaults_are_set()
    {
        var config = Configure.With(_ => _.EndpointName("UnitTests").AssembliesToScan(new[] {GetType().Assembly}))
            .DefaultBuilder();

        using (var store = new DocumentStore())
        {
            store.Initialize();
            config.UsePersistence<RavenDB>(_ => _.SetDefaultDocumentStore(store));
            // TODO .ApplyRavenDBConventions(store);

            Assert.AreEqual("http://localhost:8080", store.Url);
            Assert.AreEqual("UnitTests", store.DefaultDatabase);
            // TODO Assert.AreEqual(RavenPersistenceConstants.DefaultResourceManagerId, store.ResourceManagerId);
            Assert.AreEqual(typeof(NoOpLogManager), Raven.Abstractions.Logging.LogManager.CurrentLogManager.GetType());

            Assert.IsTrue(config.Configurer.HasComponent<IDocumentStore>());
            Assert.IsTrue(config.Configurer.HasComponent<RavenSessionFactory>());
            Assert.IsTrue(config.Configurer.HasComponent<RavenUnitOfWork>());
        }

    }


}