﻿using Demonstrator.Core.Interfaces.Services.Fhir;
using Demonstrator.Models.Core.Models;
using Demonstrator.Models.Nrls;
using Demonstrator.NRLSAdapter.Helpers;
using Demonstrator.NRLSAdapter.Helpers.Models;
using Demonstrator.NRLSAdapter.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using SystemTasks = System.Threading.Tasks;

namespace Demonstrator.NRLSAdapter.DocumentReferences
{
    public class DocumentReferenceServices : IDocumentReferenceServices
    {
        private readonly ExternalApiSetting _spineSettings;
        private readonly ApiSetting _apiSettings;
        private readonly IMemoryCache _cache;

        public DocumentReferenceServices(IOptions<ExternalApiSetting> externalApiSetting, IOptions<ApiSetting> apiSetting, IMemoryCache cache)
        {
            _spineSettings = externalApiSetting.Value;
            _apiSettings = apiSetting.Value;
            _cache = cache;
        }

        public async SystemTasks.Task<Resource> GetPointersBundle(NrlsPointerRequest pointerRequest)
        {
            var pointers = await new FhirConnector().RequestOne<Resource>(BuildGetRequest(pointerRequest.Asid, pointerRequest.NhsNumber, pointerRequest.CustodianOrgCode, pointerRequest.PointerId, pointerRequest.TypeCode, pointerRequest.JwtOrgCode));

            return pointers;
        }

        public async SystemTasks.Task<NrlsCreateResponse> GenerateAndCreatePointer(NrlsPointerRequest pointerRequest)
        {
            //update to allow seperate org codes for custodians and authors
            var pointer = NrlsPointer.Generate(_spineSettings.NrlsDefaultprofile, pointerRequest.CustodianOrgCode, pointerRequest.NhsNumber, pointerRequest.RecordUrl, pointerRequest.RecordContentType, pointerRequest.TypeCode, pointerRequest.TypeDisplay);

            var newPointer = await CreatePointer(pointerRequest, pointer);

            return newPointer;
        }

        public async SystemTasks.Task<NrlsCreateResponse> CreatePointer(NrlsPointerRequest pointerRequest, DocumentReference pointer)
        {
            var pointerJson = new FhirJsonSerializer().SerializeToString(pointer);
            var content = new StringContent(pointerJson, Encoding.UTF8, $"{ContentType.JSON_CONTENT_HEADER }; charset={Encoding.UTF8.WebName}");

            var newPointer = await new FhirConnector().RequestOne(BuildPostRequest(pointerRequest.Asid, pointerRequest.JwtOrgCode, content));

            var createResponse = new NrlsCreateResponse
            {
                Resource = newPointer.GetResource<OperationOutcome>(),
                ResponseLocation = newPointer.ResponseLocation
            };

            return createResponse;
        }

        public async SystemTasks.Task<OperationOutcome> DeletePointer(NrlsPointerRequest pointerRequest)
        {
            var pointer = await new FhirConnector().RequestOne<OperationOutcome>(BuildDeleteRequest(pointerRequest.Asid, pointerRequest.JwtOrgCode, pointerRequest.PointerId));

            return pointer;
        }

        private CommandRequest BuildGetRequest(string asid, string nhsNumber, string custodianOrgCode, string pointerId, string typeCode, string jwtOrgCode)
        {
            return BuildRequest(asid, pointerId, nhsNumber, custodianOrgCode, typeCode, jwtOrgCode, HttpMethod.Get, null);
        }

        private CommandRequest BuildPostRequest(string asid, string jwtOrgCode, HttpContent content)
        {
            
            return BuildRequest(asid, null, null, null, null, jwtOrgCode, HttpMethod.Post, content);
        }

        private CommandRequest BuildDeleteRequest(string asid, string jwtOrgCode, string pointerId)
        {
            return BuildRequest(asid, pointerId, null, null, null, jwtOrgCode, HttpMethod.Delete, null);
        }

        private CommandRequest BuildRequest(string asid, string resourceId, string nhsNumber, string custodianOrgCode, string typeCode, string jwtOrgCode, HttpMethod method, HttpContent content)
        {
            var command = new CommandRequest
            {
                BaseUrl = $"{(_spineSettings.NrlsUseSecure ? _spineSettings.NrlsSecureServerUrl : _spineSettings.NrlsServerUrl)}",
                ResourceId = resourceId,
                ResourceType = ResourceType.DocumentReference,
                SearchParams = GetParams(nhsNumber, custodianOrgCode, resourceId, typeCode),
                Method = method,
                Content = content,
                UseSecure = _spineSettings.NrlsUseSecure,
                ClientThumbprint = ClientSettings(asid)?.Thumbprint,
                ServerThumbprint = _spineSettings.SpineThumbprint
            };

            var jwt = JwtFactory.Generate(method == HttpMethod.Get ? JwtScopes.Read : JwtScopes.Write, jwtOrgCode, "fakeRoleId", asid, command.FullUrl.AbsoluteUri, SystemUrlBase);

            command.Headers.Add(HeaderNames.Authorization, $"Bearer {jwt}");
            command.Headers.Add(FhirConstants.HeaderFromAsid, asid);
            command.Headers.Add(FhirConstants.HeaderToAsid, _spineSettings.SpineAsid);

            return command;
        }

        private SearchParams GetParams(string nhsNumber, string orgCode, string id, string typeCode)
        {
            var searchParams = new SearchParams();

            if (!string.IsNullOrWhiteSpace(orgCode))
            {
                searchParams.Add("custodian", $"{WebUtility.UrlEncode(FhirConstants.SystemODS)}{orgCode}");
            }

            if (!string.IsNullOrWhiteSpace(nhsNumber))
            {
                searchParams.Add("subject", $"{WebUtility.UrlEncode(FhirConstants.SystemPDS)}{nhsNumber}");
            }

            if (!string.IsNullOrWhiteSpace(typeCode))
            {
                searchParams.Add("type.coding", $"{WebUtility.UrlEncode(FhirConstants.SystemType)}|{typeCode}");
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                searchParams.Add("_id", id);
            }

            return searchParams;
        }

        private ClientAsid ClientSettings(string asid)
        {
            var map = _cache.Get<ClientAsidMap>(ClientAsidMap.Key);

            return map.ClientAsids.FirstOrDefault(x => !string.IsNullOrEmpty(asid) && x.Key == asid).Value;
        }

        private string SystemUrlBase
        {
            get
            {
                return $"{(_apiSettings.Secure ? "https" : "http")}{_apiSettings.BaseUrl}:{(_apiSettings.Secure ? _apiSettings.SecurePort : _apiSettings.DefaultPort)}";
            }
        }
    }
}
