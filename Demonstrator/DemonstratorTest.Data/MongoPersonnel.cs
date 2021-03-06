﻿using Demonstrator.Models.Core.Models;
using Demonstrator.Models.DataModels.Flows;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace DemonstratorTest.Data.Helpers
{
    public static class MongoPersonnel
    {
        public static IList<Personnel> Personnel
        {
            get
            {
                return new List<Personnel>
                {
                    new Personnel
                    {
                        Id = new ObjectId("5a8417f68317338c8e080a62"),
                        Name = "999 Call Handler",
                        ImageUrl = "....",
                        Context = new List<ContentView>()
                        {
                            new ContentView
                            {
                                Title = "Title Text",
                                Content = new List<string>{"Content Text" },
                                CssClass = "CssClass Text",
                                Order = 1
                            }
                        },
                        UsesNrls = true,
                        OrganisationId = "5a82f9ffcb969daa58d33377",
                        CModule = "CModule-Type",
                        SystemIds = new List<string> { "5a8417338317338c8e0809e5" },
                        IsActive = true,
                        CreatedOn = DateTime.Parse("2018-02-08T10:00:00"),
                        Benefits = new List<string> { "benefitid" }
                    }
                };
            }
        }

    }
}
