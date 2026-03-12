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
    /// The <see cref="GetIntegrationInstanceActionMock"/> class.
    /// </summary>
    public class GetIntegrationInstanceActionMock : ActionMock
    {
        /// <summary>
        /// Creates a mocked instance for  <see cref="GetIntegrationInstanceActionMock"/> with static outputs.
        /// </summary>
        public GetIntegrationInstanceActionMock(TestWorkflowStatus status = TestWorkflowStatus.Succeeded, string name = null, GetIntegrationInstanceActionOutput outputs = null)
            : base(status: status, name: name, outputs: outputs ?? new GetIntegrationInstanceActionOutput())
        {
        }

        /// <summary>
        /// Creates a mocked instance for  <see cref="GetIntegrationInstanceActionMock"/> with static error info.
        /// </summary>
        public GetIntegrationInstanceActionMock(TestWorkflowStatus status, string name = null, TestErrorInfo error = null)
            : base(status: status, name: name, error: error)
        {
        }

        /// <summary>
        /// Creates a mocked instance for <see cref="GetIntegrationInstanceActionMock"/> with a callback function for dynamic outputs.
        /// </summary>
        public GetIntegrationInstanceActionMock(Func<TestExecutionContext, GetIntegrationInstanceActionMock> onGetActionMock, string name = null)
            : base(onGetActionMock: onGetActionMock, name: name)
        {
        }
    }

    /// <summary>
    /// Class for GetIntegrationInstanceActionOutput representing an object with properties.
    /// </summary>
    public class GetIntegrationInstanceActionOutput : MockOutput
    {
        public HttpStatusCode StatusCode {get; set;}

        public JObject Body { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIntegrationInstanceActionOutput"/> class.
        /// </summary>
        public GetIntegrationInstanceActionOutput()
        {
            this.StatusCode = HttpStatusCode.OK;
            this.Body = new JObject();
        }
    }
}