using ArmoniK.Attributes;
using ArmoniK.Core.gRPC.V1;
using ArmoniK.DevelopmentKit.WorkerApi.Common;

using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

using ArmoniK.DevelopmentKit.Common;
using ArmoniK.DevelopmentKit.Common.Exceptions;

using Google.Protobuf.Collections;

#pragma warning disable CS1591

namespace ArmoniK.DevelopmentKit.GridServer
{
    [XmlDocIgnore]
    public class GridWorker : IGridWorker
    {
        private ILogger<GridWorker> Logger { get; set; }
        
        public ILoggerFactory LoggerFactory { get; set; }
        public GridWorker(IConfiguration configuration, LoggerFactory factory)
        {
            Configuration = configuration;
            LoggerFactory = factory;
            Logger        = factory.CreateLogger<GridWorker>();

        }

        public IConfiguration Configuration { get; set; }

        public void Configure(IConfiguration configuration, IDictionary<string, string> clientOptions,
            AppsLoader appsLoader)
        {
            Configurations = configuration;
            ClientServiceOptions = clientOptions;


            GridAppName = clientOptions[AppsOptions.GridAppNameKey];
            GridAppVersion = clientOptions[AppsOptions.GridAppVersionKey];
            GridAppNamespace = clientOptions[AppsOptions.GridAppNamespaceKey];
            GridServiceName = clientOptions[AppsOptions.GridServiceNameKey];

            ServiceClass = appsLoader.GetServiceContainerInstance<object>(GridAppNamespace,
                GridServiceName);
        }

        public object ServiceClass { get; set; }

        public string GridServiceName { get; set; }

        public string GridAppNamespace { get; set; }

        public string GridAppVersion { get; set; }

        public string GridAppName { get; set; }

        public IDictionary<string, string> ClientServiceOptions { get; set; }

        public IConfiguration Configurations { get; set; }

        public void InitializeSessionWorker(string session, IDictionary<string, string> requestTaskOptions)
        {
            if (session == null)
                throw new ArgumentNullException($"Session is null in the Execute function");

            ServiceInvocationContext ??= new ServiceInvocationContext()
            {
                SessionId = session.UnPackSessionId()
            };

            TaskOptions serviceAdminTaskOptions = new()
            {
                IdTag = "ServiceAdminTask-",
                MaxDuration = new Duration()
                {
                    Seconds = 3600,
                },
                MaxRetries = 5,
                Priority = 1,
            };

            ServiceAdminWorker = new ServiceAdminWorker(Configurations, LoggerFactory,
                serviceAdminTaskOptions);
        }

        public ServiceAdminWorker ServiceAdminWorker { get; set; }

        public ServiceInvocationContext ServiceInvocationContext { get; set; }


        public byte[] Execute(string session, ComputeRequest request)
        {
            byte[] payload = request.Payload.ToByteArray();

            DataSynapsePayload dataSynapsePayload = DataSynapsePayload.Deserialize(payload);

            if (dataSynapsePayload.DataSynapseRequestType != DataSynapseRequestType.Execute)
            {
                return RequestTypeBalancer(dataSynapsePayload);
            }

            payload = dataSynapsePayload.ClientPayload;

            var functionData = new ProtoSerializer().DeSerializeMessageObjectArray(payload);

            string methodName;
            if (functionData != null && functionData.Length >= 1)
            {
                methodName = Convert.ToString(functionData[0]);
            }
            else
            {
                throw new WorkerApiException(
                    $"The method name of class {ServiceClass} is not found in the submit function");
            }

            object[] arguments = functionData[1] as object[];

            if (methodName == null)
                throw new WorkerApiException(
                    $"Method name is empty in Service class [{GridAppNamespace}.{GridServiceName}]");

            var methodInfo = ServiceClass.GetType().GetMethod(methodName);

            if (methodInfo == null)
                throw new WorkerApiException(
                    $"Cannot found method [{methodName}] in Service class [{GridAppNamespace}.{GridServiceName}]");

            try
            {
                object result = methodInfo.Invoke(ServiceClass,
                    arguments);
                if (result != null)
                {
                    return new ProtoSerializer().SerializeMessageObjectArray(new[] { result });
                }
            }
            catch (TargetException e)
            {
                throw new WorkerApiException(e);
            }
            catch (ArgumentException e)
            {
                throw new WorkerApiException(e);
            }
            catch (TargetInvocationException e)
            {
                throw new WorkerApiException(e.InnerException);
            }
            catch (TargetParameterCountException e)
            {
                throw new WorkerApiException(e);
            }
            catch (MethodAccessException e)
            {
                throw new WorkerApiException(e);
            }
            catch (InvalidOperationException e)
            {
                throw new WorkerApiException(e);
            }
            catch (Exception e)
            {
                throw new WorkerApiException(e);
            }


            return new byte[] { };
        }

        private byte[] RequestTypeBalancer(DataSynapsePayload dataSynapsePayload)
        {
            switch (dataSynapsePayload.DataSynapseRequestType)
            {
                case DataSynapseRequestType.Upload:
                    return ServiceAdminWorker.UploadResources("TODO");
                default:
                    return new byte[] { };
            }
        }

        public void SessionFinalize()
        {
            ServiceInvocationContext = null;
        }

        public void DestroyService()
        {
          Dispose();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            SessionFinalize();
            ServiceAdminWorker.Dispose();
        }
    }
}