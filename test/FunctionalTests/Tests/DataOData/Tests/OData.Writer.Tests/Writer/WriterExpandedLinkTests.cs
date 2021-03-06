﻿//---------------------------------------------------------------------
// <copyright file="WriterExpandedLinkTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Test.Taupo.OData.Writer.Tests.Writer
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.OData.Core;
    using Microsoft.OData.Edm;
    using Microsoft.Test.OData.Utils.CombinatorialEngine;
    using Microsoft.Test.Taupo.Common;
    using Microsoft.Test.Taupo.Execution;
    using Microsoft.Test.Taupo.OData.Atom;
    using Microsoft.Test.Taupo.OData.Common;
    using Microsoft.Test.Taupo.OData.Json;
    using Microsoft.Test.Taupo.OData.Writer.Tests.Common;
    using Microsoft.Test.Taupo.OData.Writer.Tests.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for writing expanded links with the OData writer.
    /// </summary>
    [TestClass, TestCase]
    public class WriterExpandedLinkTests : ODataWriterTestCase
    {
        private static readonly Uri ServiceDocumentUri = new Uri("http://odata.org/");

        [InjectDependency(IsRequired = true)]
        public PayloadWriterTestDescriptor.Settings Settings { get; set; }

        [TestMethod, Variation(Description = "Test that we cannot write an expanded link with incorrect multiplicity or content (entry vs. feed).")]
        public void ExpandedLinkWithMultiplicityTests()
        {
            ODataNavigationLink expandedEntryLink = ObjectModelUtils.CreateDefaultCollectionLink();
            expandedEntryLink.IsCollection = false;

            ODataNavigationLink expandedFeedLink = ObjectModelUtils.CreateDefaultCollectionLink();
            expandedFeedLink.IsCollection = true;

            ODataEntry defaultEntry = ObjectModelUtils.CreateDefaultEntry();
            ODataFeed defaultFeed = ObjectModelUtils.CreateDefaultFeed();
            ODataEntityReferenceLink defaultEntityReferenceLink = ObjectModelUtils.CreateDefaultEntityReferenceLink();

            ODataEntry officeEntry = ObjectModelUtils.CreateDefaultEntry("TestModel.OfficeType");
            ODataEntry officeWithNumberEntry = ObjectModelUtils.CreateDefaultEntry("TestModel.OfficeWithNumberType");
            ODataEntry cityEntry = ObjectModelUtils.CreateDefaultEntry("TestModel.CityType");

            // CityHall is a nav prop with multiplicity '*' of type 'TestModel.OfficeType'
            ODataNavigationLink cityHallLinkIsCollectionNull = ObjectModelUtils.CreateDefaultCollectionLink("CityHall", /*isCollection*/ null);
            ODataNavigationLink cityHallLinkIsCollectionTrue = ObjectModelUtils.CreateDefaultCollectionLink("CityHall", /*isCollection*/ true);
            ODataNavigationLink cityHallLinkIsCollectionFalse = ObjectModelUtils.CreateDefaultCollectionLink("CityHall", /*isCollection*/ false);

            // PoliceStation is a nav prop with multiplicity '1' of type 'TestModel.OfficeType'
            ODataNavigationLink policeStationLinkIsCollectionNull = ObjectModelUtils.CreateDefaultCollectionLink("PoliceStation", /*isCollection*/ null);
            ODataNavigationLink policeStationLinkIsCollectionTrue = ObjectModelUtils.CreateDefaultCollectionLink("PoliceStation", /*isCollection*/ true);
            ODataNavigationLink policeStationLinkIsCollectionFalse = ObjectModelUtils.CreateDefaultCollectionLink("PoliceStation", /*isCollection*/ false);

            ExpectedException expandedEntryLinkWithFeedContentError = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkIsCollectionFalseWithFeedContent", "http://odata.org/link");
            ExpectedException expandedFeedLinkWithEntryContentError = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkIsCollectionTrueWithEntryContent", "http://odata.org/link");
            ExpectedException expandedFeedLinkWithEntryMetadataError = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkIsCollectionTrueWithEntryMetadata", "http://odata.org/link");

            ExpectedException expandedEntryLinkWithFeedMetadataErrorResponse = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkIsCollectionFalseWithFeedMetadata", "http://odata.org/link");
            ExpectedException expandedFeedLinkPayloadWithEntryMetadataErrorRequest = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkWithFeedPayloadAndEntryMetadata", "http://odata.org/link");
            ExpectedException expandedFeedLinkPayloadWithEntryMetadataErrorResponse = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkIsCollectionTrueWithEntryMetadata", "http://odata.org/link");
            ExpectedException expandedEntryLinkPayloadWithFeedMetadataErrorResponse = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkIsCollectionFalseWithFeedMetadata", "http://odata.org/link");
            ExpectedException expandedEntryLinkPayloadWithFeedMetadataError = ODataExpectedExceptions.ODataException("WriterValidationUtils_ExpandedLinkWithEntryPayloadAndFeedMetadata", "http://odata.org/link");
            ExpectedException multipleItemsInExpandedLinkError = ODataExpectedExceptions.ODataException("ODataWriterCore_MultipleItemsInNavigationLinkContent", "http://odata.org/link");
            ExpectedException entityReferenceLinkInResponseError = ODataExpectedExceptions.ODataException("ODataWriterCore_EntityReferenceLinkInResponse");

            IEdmModel model = Microsoft.Test.OData.Utils.Metadata.TestModels.BuildTestModel();

            var testCases = new ExpandedLinkMultiplicityTestCase[]
                {
                    #region IsCollection flag does not match payload
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link with IsCollection is 'false' and feed payload
                        Items = new ODataItem[] { defaultEntry, expandedEntryLink, defaultFeed },
                        ExpectedError = tc => expandedEntryLinkWithFeedContentError,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link with IsCollection is 'true' and entry payload
                        Items = new ODataItem[] { defaultEntry, expandedFeedLink, defaultEntry },
                        ExpectedError = tc => expandedFeedLinkWithEntryContentError,
                    },
                    #endregion IsCollection flag does not match payload
                    #region IsCollection == null; check compatibility of entity types of navigation property and entity in expanded link
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link of singleton type without IsCollection value and an entry of a non-matching entity type;
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionNull, cityEntry },
                        ExpectedError = tc => tc.Format == ODataFormat.Atom || !tc.IsRequest
                            ? ODataExpectedExceptions.ODataException("WriterValidationUtils_EntryTypeInExpandedLinkNotCompatibleWithNavigationPropertyType", "TestModel.CityType", "TestModel.OfficeType")
                            : ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "PoliceStation"),
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link of singleton type without IsCollection value and an entry of a matching entity type; no error expected.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionNull, officeEntry },
                        ExpectedError = tc => tc.Format == ODataFormat.Json && !tc.IsRequest ? null : ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "PoliceStation"),
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link of singleton type without IsCollection and an entry of a derived entity type; no error expected.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionNull, officeWithNumberEntry },
                        ExpectedError = tc => tc.Format == ODataFormat.Json && !tc.IsRequest ? null : ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "PoliceStation"),
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link of collection type without IsCollection value and an entry of a non-matching entity type;
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionNull, defaultFeed, cityEntry },
                        ExpectedError = tc => tc.Format == ODataFormat.Json && !tc.IsRequest 
                            ? ODataExpectedExceptions.ODataException("WriterValidationUtils_EntryTypeInExpandedLinkNotCompatibleWithNavigationPropertyType", "TestModel.CityType", "TestModel.OfficeType")
                            : ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "CityHall"),
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link of collection type without IsCollection value and an entry of a matching entity type; no error expected.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionNull, defaultFeed, officeEntry },
                        ExpectedError = tc => tc.Format == ODataFormat.Json && !tc.IsRequest 
                            ? null 
                            : ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "CityHall"),
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Expanded link of collection type without IsCollection and an entry of a derived entity type; no error expected.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionNull, defaultFeed, officeWithNumberEntry },
                        ExpectedError = tc => tc.Format == ODataFormat.Json && !tc.IsRequest 
                            ? null 
                            : ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "CityHall"),
                        Model = model,
                    },
                    #endregion IsCollection == null; check compatibility of entity types of navigation property and entity in expanded link
                    #region Expanded link with entry content
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Entry content, IsCollection == false, singleton nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionFalse, officeEntry },
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Entry content, IsCollection == true, singleton nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionTrue, officeEntry },
                        ExpectedError = tc => expandedFeedLinkWithEntryContentError,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Entry content, IsCollection == false, collection nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionFalse, officeEntry },
                        ExpectedError = tc => tc.IsRequest ? expandedEntryLinkPayloadWithFeedMetadataError : expandedEntryLinkPayloadWithFeedMetadataErrorResponse,
                        Model = model
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Entry content, IsCollection == true, collection nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionTrue, officeEntry },
                        ExpectedError = tc => expandedFeedLinkWithEntryContentError,
                        Model = model
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Entry content, IsCollection == null, singleton nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionNull, officeEntry },
                        ExpectedError = tc => tc.IsRequest || tc.Format == ODataFormat.Atom
                            ? ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "PoliceStation")
                            : null,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Entry content, IsCollection == null, collection nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionNull, officeEntry },
                        ExpectedError = tc => expandedEntryLinkPayloadWithFeedMetadataError,
                        Model = model,
                    },
                    #endregion Expanded collection link with entry content
                    #region Expanded link with feed content
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Feed content, IsCollection == false, singleton nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionFalse, defaultFeed, officeEntry },
                        ExpectedError = tc => expandedEntryLinkWithFeedContentError,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Feed content, IsCollection == true, singleton nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionTrue, defaultFeed, officeEntry },
                        ExpectedError = tc => tc.IsRequest ? expandedFeedLinkPayloadWithEntryMetadataErrorRequest : expandedFeedLinkPayloadWithEntryMetadataErrorResponse,
                        Model = model
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Feed content, IsCollection == false, collection nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionFalse, defaultFeed, officeEntry },
                        ExpectedError = tc => tc.IsRequest ? expandedEntryLinkWithFeedContentError : expandedEntryLinkWithFeedMetadataErrorResponse,
                        Model = model
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Feed content, IsCollection == true, collection nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionTrue, defaultFeed, officeEntry },
                        Model = model
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Feed content, IsCollection == null, singleton nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionNull, defaultFeed, officeEntry },
                        ExpectedError = tc => expandedFeedLinkPayloadWithEntryMetadataErrorRequest,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Feed content, IsCollection == null, collection nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionNull, defaultFeed, officeEntry },
                        ExpectedError = tc => tc.IsRequest || tc.Format == ODataFormat.Atom
                            ? ODataExpectedExceptions.ODataException("WriterValidationUtils_NavigationLinkMustSpecifyIsCollection", "CityHall")
                            : null,
                        Model = model,
                    },
                    #endregion Expanded collection link with entry content
                    #region Expanded link with entity reference link content
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Single ERL (entity reference link) content, IsCollection == false, singleton nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionFalse, defaultEntityReferenceLink },
                        ExpectedError = tc => tc.IsRequest ? null : entityReferenceLinkInResponseError
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Multiple ERL (entity reference link) content, IsCollection == false, singleton nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionFalse, defaultEntityReferenceLink, defaultEntityReferenceLink },
                        ExpectedError = tc => tc.IsRequest ? multipleItemsInExpandedLinkError : entityReferenceLinkInResponseError,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Single ERL content, IsCollection == true, singleton nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionTrue, defaultEntityReferenceLink },
                        ExpectedError = tc => expandedFeedLinkWithEntryMetadataError,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Multiple ERL content, IsCollection == true, singleton nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, policeStationLinkIsCollectionTrue, defaultEntityReferenceLink, defaultEntityReferenceLink },
                        ExpectedError = tc => expandedFeedLinkWithEntryMetadataError,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Single ERL content, IsCollection == false, collection nav prop; should not fail (metadata mismatch explicitly allowed).
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionFalse, defaultEntityReferenceLink },
                        ExpectedError = tc => tc.IsRequest ? null : expandedEntryLinkWithFeedMetadataErrorResponse,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Multiple ERL content, IsCollection == false, collection nav prop; should fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionFalse, defaultEntityReferenceLink, defaultEntityReferenceLink },
                        ExpectedError = tc => tc.IsRequest ? multipleItemsInExpandedLinkError : expandedEntryLinkWithFeedMetadataErrorResponse,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Single ERL content, IsCollection == true, collection nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionTrue, defaultEntityReferenceLink },
                        ExpectedError = tc => tc.IsRequest ? null : entityReferenceLinkInResponseError,
                        Model = model,
                    },
                    new ExpandedLinkMultiplicityTestCase
                    {
                        // Multiple ERL content, IsCollection == true, collection nav prop; should not fail.
                        Items = new ODataItem[] { cityEntry, cityHallLinkIsCollectionTrue, defaultEntityReferenceLink, defaultEntityReferenceLink },
                        ExpectedError = tc => tc.IsRequest ? null : entityReferenceLinkInResponseError,
                        Model = model,
                    },

                    //// NOTE: Not testing the cases where IsCollection == null here since ERL payloads are only allowed in
                    ////       requests where IsCollection is required (in ATOM and JSON)
                    #endregion Expanded link with entity reference link content
                };

            // TODO: Fix places where we've lost JsonVerbose coverage to add JsonLight
            this.CombinatorialEngineProvider.RunCombinations(
                testCases,
                this.WriterTestConfigurationProvider.ExplicitFormatConfigurations.Where(tc => tc.Format == ODataFormat.Atom),
                (testCase, testConfiguration) =>
                {
                    testConfiguration = testConfiguration.Clone();
                    testConfiguration.MessageWriterSettings.SetServiceDocumentUri(ServiceDocumentUri);

                    using (var memoryStream = new TestStream())
                    using (var messageWriter = TestWriterUtils.CreateMessageWriter(memoryStream, testConfiguration, this.Assert, null, testCase.Model))
                    {
                        ODataWriter writer = messageWriter.CreateODataWriter(isFeed: false);
                        TestExceptionUtils.ExpectedException(
                            this.Assert,
                            () => TestWriterUtils.WritePayload(messageWriter, writer, true, testCase.Items),
                            testCase.ExpectedError == null ? null : testCase.ExpectedError(testConfiguration),
                            this.ExceptionVerifier);
                    }
                });
        }

        [TestMethod, Variation(Description = "Test that we can write an expanded link with a null navigation entry.")]
        public void ExpandedLinkWithNullNavigationTests()
        {
            ODataNavigationLink expandedEntryLink = ObjectModelUtils.CreateDefaultCollectionLink();
            expandedEntryLink.IsCollection = false;

            ODataNavigationLink expandedEntryLink2 = ObjectModelUtils.CreateDefaultCollectionLink();
            expandedEntryLink2.IsCollection = false;
            expandedEntryLink2.Name = expandedEntryLink2.Name + "2";

            ODataEntry defaultEntry = ObjectModelUtils.CreateDefaultEntry();
            ODataFeed defaultFeed = ObjectModelUtils.CreateDefaultFeed();
            ODataEntry nullEntry = ObjectModelUtils.ODataNullEntry;

            PayloadWriterTestDescriptor.WriterTestExpectedResultCallback successCallback = (testConfiguration) =>
            {
                if (testConfiguration.Format == ODataFormat.Atom)
                {
                    return new AtomWriterTestExpectedResults(this.Settings.ExpectedResultSettings)
                    {
                        Xml = new XElement(TestAtomConstants.ODataMetadataXNamespace + TestAtomConstants.ODataInlineElementName).ToString(),
                        FragmentExtractor = (result) =>
                            {
                                return result.Elements(TestAtomConstants.AtomXNamespace + TestAtomConstants.AtomLinkElementName)
                                             .First(e => e.Element(TestAtomConstants.ODataMetadataXNamespace + TestAtomConstants.ODataInlineElementName) != null)
                                             .Element(TestAtomConstants.ODataMetadataXNamespace + TestAtomConstants.ODataInlineElementName);
                            }
                    };
                }
                else
                {
                    return new JsonWriterTestExpectedResults(this.Settings.ExpectedResultSettings)
                    {
                        Json = "null", //new JsonPrimitiveValue(null).ToText(testConfiguration.MessageWriterSettings.Indent),
                        FragmentExtractor = (result) =>
                        {
                            return JsonUtils.UnwrapTopLevelValue(testConfiguration, result).Object().Properties.First(p => p.Name == ObjectModelUtils.DefaultLinkName).Value;
                        }
                    };
                }

            };

            Func<ExpectedException,PayloadWriterTestDescriptor.WriterTestExpectedResultCallback> errorCallback = (expectedException) =>
                {
                    return (testConfiguration) =>
                        {
                            return new WriterTestExpectedResults(this.Settings.ExpectedResultSettings)
                            {
                                ExpectedException2 = expectedException,
                            };
                        };
                };

            var testCases = new PayloadWriterTestDescriptor<ODataItem>[]
                {
                    // navigation to a null entry
                    new PayloadWriterTestDescriptor<ODataItem>(
                        this.Settings,
                        new ODataItem[] { defaultEntry, expandedEntryLink, nullEntry },
                        successCallback),

                    // navigation to a null entry twice
                    new PayloadWriterTestDescriptor<ODataItem>(
                        this.Settings,
                        new ODataItem[] {  defaultEntry, expandedEntryLink, nullEntry, null, null, expandedEntryLink2, nullEntry, null },
                        successCallback),

                    // top level null entry.
                    new PayloadWriterTestDescriptor<ODataItem>(
                        this.Settings,
                        new ODataItem[] { nullEntry },
                        errorCallback(new ExpectedException(typeof(ArgumentNullException)))),

                    // navigation to a null entry twice in expanded link
                    // this actually throws ArgumentNullException when WriteStart() for the second nullEntry is called since
                    // the state has been changed from NavigationLink to ExpandedLink after the first one.
                    // TODO: check if ArgumentNullException needs to change the WriterState.
                    new PayloadWriterTestDescriptor<ODataItem>(
                        this.Settings,
                        new ODataItem[] { defaultEntry, expandedEntryLink, nullEntry, null, nullEntry },
                        errorCallback(new ExpectedException(typeof(ArgumentNullException)))),

                    // Null entry inside a feed, same as above this throws ArgumentNullException but state is not put to error state.
                    new PayloadWriterTestDescriptor<ODataItem>(
                        this.Settings,
                        new ODataItem[] { defaultFeed, nullEntry },
                        errorCallback(new ExpectedException(typeof(ArgumentNullException)))),
                };

            // TODO: Fix places where we've lost JsonVerbose coverage to add JsonLight
            this.CombinatorialEngineProvider.RunCombinations(
                testCases,
                this.WriterTestConfigurationProvider.ExplicitFormatConfigurationsWithIndent.Where(tc => tc.Format == ODataFormat.Atom),
                (testCase, testConfig) =>
                {
                    testConfig = testConfig.Clone();
                    testConfig.MessageWriterSettings.SetServiceDocumentUri(ServiceDocumentUri);

                    TestWriterUtils.WriteAndVerifyODataPayload(testCase, testConfig, this.Assert, this.Logger);
                });
        }

        private sealed class ExpandedLinkMultiplicityTestCase
        {
            public ODataItem[] Items { get; set; }
            public IEdmModel Model { get; set; }
            public Func<WriterTestConfiguration, ExpectedException> ExpectedError { get; set; }
        }
    }
}
