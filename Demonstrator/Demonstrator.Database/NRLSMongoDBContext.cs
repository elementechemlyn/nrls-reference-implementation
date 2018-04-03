﻿using Demonstrator.Core.Interfaces.Database;
using Demonstrator.Models.Core.Models;
using Demonstrator.Models.DataModels.Flows;
using Demonstrator.Models.DataModels.Epr;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Demonstrator.Database
{
    public class NRLSMongoDBContext : INRLSMongoDBContext
    {
        private readonly IMongoDatabase _database = null;

        public NRLSMongoDBContext(IOptions<DbSetting> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            if (client != null)
                _database = client.GetDatabase(settings.Value.Database);
        }

        public IMongoCollection<ActorOrganisation> ActorOrganisations
        {
            get
            {
                return _database.GetCollection<ActorOrganisation>("ActorOrganisation");
            }
        }

        public IMongoCollection<GenericSystem> GenericSystems
        {
            get
            {
                return _database.GetCollection<GenericSystem>("GenericSystem");
            }
        }

        public IMongoCollection<Personnel> Personnel
        {
            get
            {
                return _database.GetCollection<Personnel>("Personnel");
            }
        }

        public IMongoCollection<NrlsPointerMapper> NrlsPointers
        {
            get
            {
                return _database.GetCollection<NrlsPointerMapper>("NrlsPointers");
            }
        }

        public IMongoCollection<CrisisPlan> CrisisPlans
        {
            get
            {
                return _database.GetCollection<CrisisPlan>("MedicalRecords");
            }
        }

        public IMongoCollection<Benefit> Benefits
        {
            get
            {
                return _database.GetCollection<Benefit>("Benefits");
            }
        }
    }
}
