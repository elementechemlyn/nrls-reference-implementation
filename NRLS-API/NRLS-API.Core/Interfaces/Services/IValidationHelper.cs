﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;

namespace NRLS_API.Core.Interfaces.Services
{
    public interface IValidationHelper
    {
        //IResourceResolver Resolver { get; }

        Validator Validator { get; }

        bool ValidCodableConcept(CodeableConcept concept, string validSystem, bool validateFromSet = false, bool systemRequired = true, bool codeRequired = true, bool displayRequired = true, string valueSet = null);

        bool ValidReference(ResourceReference reference, string startsWith);

        bool ValidIdentifier(Identifier identifier);

        string GetResourceReferenceId(ResourceReference reference, string systemUrl);

        bool ValidTokenParameter(string parameterVal, string expectedSystemPrefix = null, bool allowOptionalSystemOrValue = true);

        bool ValidReferenceParameter(string parameterVal, string systemPrefix);

        string GetTokenParameterId(string parameterVal, string systemPrefix);

        bool ValidNhsNumber(string nhsNumber);
    }
}
