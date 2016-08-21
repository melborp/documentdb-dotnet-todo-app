namespace todo
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;

    public static class DocumentDBRepository<T> where T : class
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["collection"];
        private static DocumentClient client;
        private static TelemetryClient telemetry = new TelemetryClient();

        public static async Task<T> GetItemAsync(string id)
        {
            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                watch.Stop();
                Dictionary<string, string> classificationAndFilter = new Dictionary<string, string>();
                classificationAndFilter.Add("Performance", "Performance");
                classificationAndFilter.Add("DocumentDB", "DocumentDB");
                telemetry.TrackMetric("DocumentDB.GetItem (ms)", watch.ElapsedMilliseconds, classificationAndFilter);
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1 })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }
            watch.Stop();
            Dictionary<string, string> classificationAndFilter = new Dictionary<string, string>();
            classificationAndFilter.Add("Performance", "Performance");
            classificationAndFilter.Add("DocumentDB", "DocumentDB");
            telemetry.TrackMetric("DocumentDB.GetItems (ms)", watch.ElapsedMilliseconds, classificationAndFilter);
            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var result = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
            watch.Stop();
            Dictionary<string, string> classificationAndFilter = new Dictionary<string, string>();
            classificationAndFilter.Add("Performance", "Performance");
            classificationAndFilter.Add("DocumentDB", "DocumentDB");
            telemetry.TrackMetric("DocumentDB.CreateItem (ms)", watch.ElapsedMilliseconds, classificationAndFilter);
            return result;
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var result = await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
            watch.Stop();
            Dictionary<string, string> classificationAndFilter = new Dictionary<string, string>();
            classificationAndFilter.Add("DocumentDB", "DocumentDB");
            telemetry.TrackMetric("DocumentDB.ReplaceDocument (ms)", watch.ElapsedMilliseconds, classificationAndFilter);
            return result;
        }

        public static async Task DeleteItemAsync(string id)
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]), ConfigurationManager.AppSettings["authKey"]);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}