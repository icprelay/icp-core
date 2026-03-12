using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Microsoft.Azure.Workflows.UnitTesting.ErrorResponses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System;

namespace icp_logicapp.Tests.Mocks.process
{
    /// <summary>
    /// The <see cref="ReadBlobContentActionMock"/> class.
    /// </summary>
    public class ReadBlobContentActionMock : ActionMock
    {
        /// <summary>
        /// Creates a mocked instance for  <see cref="ReadBlobContentActionMock"/> with static outputs.
        /// </summary>
        public ReadBlobContentActionMock(TestWorkflowStatus status = TestWorkflowStatus.Succeeded, string name = null, ReadBlobContentActionOutput outputs = null)
            : base(status: status, name: name, outputs: outputs ?? new ReadBlobContentActionOutput())
        {
        }

        /// <summary>
        /// Creates a mocked instance for  <see cref="ReadBlobContentActionMock"/> with static error info.
        /// </summary>
        public ReadBlobContentActionMock(TestWorkflowStatus status, string name = null, TestErrorInfo error = null)
            : base(status: status, name: name, error: error)
        {
        }

        /// <summary>
        /// Creates a mocked instance for <see cref="ReadBlobContentActionMock"/> with a callback function for dynamic outputs.
        /// </summary>
        public ReadBlobContentActionMock(Func<TestExecutionContext, ReadBlobContentActionMock> onGetActionMock, string name = null)
            : base(onGetActionMock: onGetActionMock, name: name)
        {
        }
    }

    /// <summary>
    /// Class for ReadBlobContentActionOutput representing an object with properties.
    /// </summary>
    public class ReadBlobContentActionOutput : MockOutput
    {
        public HttpStatusCode StatusCode {get; set;}

        /// <summary>
        /// The response from the read blob action.
        /// </summary>
        public ReadBlobContentActionOutputBody Body { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadBlobContentActionOutput"/> class.
        /// </summary>
        public ReadBlobContentActionOutput()
        {
            this.StatusCode = HttpStatusCode.OK;
            this.Body = new ReadBlobContentActionOutputBody();
        }
    }
    /// <summary>
    /// The response from the read blob action.
    /// </summary>
    public class ReadBlobContentActionOutputBody
    {
        /// <summary>
        /// The blob content.
        /// </summary>
        public JObject Content { get; set; }

        /// <summary>
        /// The blob properties.
        /// </summary>
        public ReadBlobContentActionOutputBodyProperties Properties { get; set; }

        /// <summary>
        /// The blob metadata.
        /// </summary>
        public JObject Metadata { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadBlobContentActionOutputBody"/> class.
        /// </summary>
        public ReadBlobContentActionOutputBody()
        {
            this.Content = new JObject();
            this.Properties = new ReadBlobContentActionOutputBodyProperties();
            this.Metadata = new JObject();
        }
    }
    /// <summary>
    /// The blob properties.
    /// </summary>
    public class ReadBlobContentActionOutputBodyProperties
    {
        /// <summary>
        /// The creation time for the blob.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// The blob type.
        /// </summary>
        public string BlobType { get; set; }

        /// <summary>
        /// Blob full path with container name.
        /// </summary>
        public string BlobFullPathWithContainer { get; set; }

        /// <summary>
        /// The content disposition.
        /// </summary>
        public string ContentDisposition { get; set; }

        /// <summary>
        /// The content MD5 hash.
        /// </summary>
        public string ContentMD5 { get; set; }

        /// <summary>
        /// The type of content.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The language of the content.
        /// </summary>
        public string ContentLanguage { get; set; }

        /// <summary>
        /// The ETag for the blob.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadBlobContentActionOutputBodyProperties"/> class.
        /// </summary>
        public ReadBlobContentActionOutputBodyProperties()
        {
            this.CreationTime = new DateTime();
            this.BlobType = string.Empty;
            this.BlobFullPathWithContainer = string.Empty;
            this.ContentDisposition = string.Empty;
            this.ContentMD5 = string.Empty;
            this.ContentType = string.Empty;
            this.ContentLanguage = string.Empty;
            this.ETag = string.Empty;
        }
    }
}