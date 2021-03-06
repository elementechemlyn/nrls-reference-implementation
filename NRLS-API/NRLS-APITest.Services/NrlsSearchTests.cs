using Hl7.Fhir.Model;
using Moq;
using NRLS_API.Core.Interfaces.Services;
using NRLS_API.Models.Core;
using NRLS_API.Services;
using NRLS_APITest.Comparer;
using NRLS_APITest.Data;
using System;
using System.Collections.Generic;
using SystemTasks = System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Options;
using NRLS_API.Core.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using NRLS_APITest.StubClasses;
using NRLS_API.Core.Resources;

namespace NRLS_APITest.Services
{
    public class NrlsSearchTests : IDisposable
    {

        private IFhirValidation _fhirValidation;
        private IFhirSearch _fhirSearch;
        private IOptionsSnapshot<NrlsApiSetting> _nrlsApiSettings;
        IMemoryCache _cache;

        private Bundle _expectedBundle;

        public NrlsSearchTests()
        {
            var validationMock = new Mock<IFhirValidation>();
            validationMock.Setup(op => op.ValidatePatientParameter(It.IsAny<string>())).Returns(delegate { return null; });

            validationMock.Setup(op => op.ValidateCustodianParameter(It.IsAny<string>())).Returns(delegate { return null; });
            validationMock.Setup(op => op.ValidateCustodianIdentifierParameter(It.IsAny<string>())).Returns(delegate { return null; });

            validationMock.Setup(op => op.ValidSummaryParameter(It.Is<string>(p => p == "notcount"))).Returns(delegate { return new OperationOutcome(); });
            validationMock.Setup(op => op.ValidSummaryParameter(It.Is<string>(p => p == "count"))).Returns(delegate { return null; });

            validationMock.Setup(op => op.GetOrganizationParameterId(It.Is<string>(p => p == "https://directory.spineservices.nhs.uk/STU3/Organization/TestOrgCode"))).Returns("TestOrgCode");
            validationMock.Setup(op => op.GetOrganizationParameterIdentifierId(It.Is<string>(p => p == "https://fhir.nhs.uk/Id/ods-organization-code|TestOrgCode"))).Returns("TestOrgCode");

            _fhirValidation = validationMock.Object;

            var searchPointer = NrlsPointers.Valid;
            var searchPointerList = new List<DocumentReference> { searchPointer };
            var searchBundle = FhirBundle.GetBundle<DocumentReference>(searchPointerList);
            var searchMock = new Mock<IFhirSearch>();
            searchMock.Setup(op => op.Get<DocumentReference>(It.IsAny<FhirRequest>())).Returns(SystemTasks.Task.Run(() => searchBundle as Resource));
            searchMock.Setup(op => op.Find<DocumentReference>(It.IsAny<FhirRequest>())).Returns(SystemTasks.Task.Run(() => searchBundle as Resource));


            var orgSearchBundle = FhirBundle.GetBundle<Organization>(new List<Organization> { FhirOrganizations.Valid_Organization });
            searchMock.Setup(op => op.Find<Organization>(It.IsAny<FhirRequest>())).Returns(SystemTasks.Task.Run(() => orgSearchBundle as Resource));

            _fhirSearch = searchMock.Object;


            var opts = new NrlsApiSetting
            {
                BaseUrl = "https://baseurl.com/",
                DefaultPort = "80",
                ProfileUrl = "https://profileurl.com",
                Secure = false,
                SecurePort = "443",
                SupportedContentTypes = new List<string> { "application/fhir+json" },
                SupportedResources = new List<string> { "DocumentReference" }
            };

            var mock = new Mock<IOptionsSnapshot<NrlsApiSetting>>();
            mock.Setup(op => op.Get(It.IsAny<string>())).Returns(opts);

            _nrlsApiSettings = mock.Object;

            _expectedBundle = new Bundle
            {
                Id = "b54e3ad5-04b5-4f8f-8dbd-0e41d2465b5c",
                Meta = new Meta
                {
                    LastUpdated = new DateTimeOffset(new DateTime(2018, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
                    VersionId = "b54e3ad5-04b5-4f8f-8dbd-0e41d2465b5c"
                },
                Type = Bundle.BundleType.Searchset,
                Total = 1,
                Link = new List<Bundle.LinkComponent>
                {
                    new Bundle.LinkComponent
                    {
                        Relation = "_self",
                        Url = "TestLink"
                    }
                },
                Entry = new List<Bundle.EntryComponent>
                {
                    new Bundle.EntryComponent
                    {
                        Search = new Bundle.SearchComponent
                        {
                            Mode = Bundle.SearchEntryMode.Match
                        },
                        FullUrl = $"ResourceLink",
                        Resource = new DocumentReference
                        {
                            Id = "5ab13f41957d0ad5d93a1339",
                            Status = DocumentReferenceStatus.Current,
                            Type = new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new Coding
                                    {
                                        System = "http://snomed.info/sct",
                                        Code = "736253002",
                                        Display = "Mental health crisis plan (record artifact)"
                                    }
                                }
                            },
                            Subject = new ResourceReference
                            {
                                Reference = "https://demographics.spineservices.nhs.uk/STU3/Patient/2686033207"
                            },
                            Indexed = new DateTime(2005, 12, 24, 9, 43, 41),
                            Author = new List<ResourceReference>
                            {
                                new ResourceReference
                                {
                                    Reference = "https://directory.spineservices.nhs.uk/STU3/Organization/1XR"
                                }
                            },
                            Custodian = new ResourceReference
                            {
                                Reference = "https://directory.spineservices.nhs.uk/STU3/Organization/1XR"
                            },
                            Content = new List<DocumentReference.ContentComponent>
                            {
                                new DocumentReference.ContentComponent
                                {
                                    Attachment = new Attachment
                                    {
                                        ContentType = "application/pdf",
                                        Url = "http://example.org/xds/mhd/Binary/07a6483f-732b-461e-86b6-edb665c45510.pdf",
                                        Title = "Mental health Care Plan Report",
                                        Creation = "2016-03-08T15:26:00+00:00"
                                    }
                                }
                            }
                        }
                    }
                }

            };

            var clientMapCache = new ClientAsidMap
            {
                ClientAsids = new Dictionary<string, ClientAsid>()
                {
                    { "000", new ClientAsid { Interactions = new List<string>() { FhirConstants.CreateInteractionId }, OrgCode = "TestOrgCode", Thumbprint = "TestThumbprint" } },
                    { "002", new ClientAsid { Interactions = new List<string>(), OrgCode = "TestOrgCode2", Thumbprint = "TestThumbprint" } },
                    { "003", new ClientAsid { Interactions = new List<string>(), OrgCode = "RV99", Thumbprint = "TestThumbprint" } }

                }
            };

            var cacheMock = MemoryCacheStub.MockMemoryCacheService.GetMemoryCache(clientMapCache);
            _cache = cacheMock;
        }

        public void Dispose()
        {
            _fhirValidation = null;
            _fhirSearch = null;
            _nrlsApiSettings = null;
            _expectedBundle = null;
            _cache = null;
        }


        [Fact]
        public async void Find_Read_Valid()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);

            var actualBundle = await search.Find<DocumentReference>(FhirRequests.Valid_Read) as Bundle;

            Assert.Equal(_expectedBundle, actualBundle, Comparers.ModelComparer<Bundle>());
        }

        [Fact]
        public async void Find_Read_Invalid_Id()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);

            await Assert.ThrowsAsync<HttpFhirException>(async () => { var actualBundle = await search.Find<DocumentReference>(FhirRequests.Invalid_Read_EmptyId); });

            //TODO check exception thrown
        }

        [Fact]
        public void Find_Read_Invalid_Params()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);

            Assert.ThrowsAsync<HttpFhirException>(async () => { var actual = await search.Find<DocumentReference>(FhirRequests.Invalid_Read_TooManyParams); });
        }

        [Fact]
        public async void Find_Search_Valid()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);

            var actualBundle = await search.Find<DocumentReference>(FhirRequests.Valid_Search) as Bundle;

            Assert.Equal(_expectedBundle, actualBundle, Comparers.ModelComparer<Bundle>());
        }

        [Fact]
        public void Find_Search_Invalid()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);

            Assert.ThrowsAsync<HttpFhirException>(async () => { var actual = await search.Find<DocumentReference>(FhirRequests.Invalid_Search); });
        }

        //TODO: add patient search exception
        //TODO: add custodian search exception


        [Fact]
        public async void Find_Search_Valid_Summary()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);

            var actualBundle = await search.Find<DocumentReference>(FhirRequests.Valid_Search_Summary) as Bundle;

            Assert.Equal(_expectedBundle, actualBundle, Comparers.ModelComparer<Bundle>());
        }

        [Fact]
        public void Find_Search_Invalid_Summary()
        {
            var search = new NrlsSearch(_nrlsApiSettings, _fhirSearch, _cache, _fhirValidation);


            Assert.ThrowsAsync<HttpFhirException>(async () => 
            {
                var actualBundle = await search.Find<DocumentReference>(FhirRequests.Invalid_Search_IncorrectSummary);
            });

        }

    }
}
